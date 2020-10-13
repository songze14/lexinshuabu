using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Threading;
using System.Data.SqlClient;
using System.Reflection;

namespace weixinshuabu.Controllers
{

    public class weixinshuabuController : Controller
    {
        //服务器数据库设置
        static SqlConnection connection = new SqlConnection("data source=172_17_0_16;database=data_core;user id=sa;password=Szl15726");
        public static Thread WorkThread = new Thread(Work_thread);
        static IList<time_plan> All_time_plan = new List<time_plan>();

        #region 集合类
        class login
        {
            public string appType = " 6";
            public string clientId = "88888";
            public string loginName { set; get; }
            public string password { set; get; }
            public string roleType = " 0";
        }
        class time_plan
        {
            public int id { set; get; }
            public string time { set; get; }
            public string usr_name { set; get; }
            public string usr_password { set; get; }
            public int maxstep { set; get; }
            public int minstep { set; get; }
        }
        //登录成功后返回的数据对象
        class Json_secon
        {
            public int code { set; get; }
            public string msg { set; get; }
            public string data { set; get; }
            public string userId { set; get; }
            public string accessToken { set; get; }//校验用的cooke
        }
        //登陆成功后返回数据中data详情
        //目前只用这两个数据
        class Json_data
        {
            public string userId { set; get; }
            public string accessToken { set; get; }//校验用的cooke
        }
        #endregion

        public ActionResult Index()
        {
            return View();
        }
        /// <summary>
        /// 加入刷步计划
        /// </summary>
        /// <param name="usrname"></param>
        /// <param name="userpass"></param>
        /// <param name="max_step"></param>
        /// <param name="min_step"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        public JsonResult Send_timeplan_thread(string usrname, string userpass, string min_step, string max_step, string time)
        {
            try
            {
                string sql = "select id,time from time_plan where usr_name=" + usrname;
                DataSet ds = new DataSet();
                ds = Query(sql);
                IList<time_plan> sle_time_plan = DataSetToEntityList<time_plan>(ds, 0);
                if (int.Parse(max_step)<int.Parse(min_step))
                {
                     return Json(new { msg = "最小步数小于最大步数，别想这种改浏览器内存" });
                }
                //插入前先验证账号密码是否正确
                login data = new login();

                data.loginName = usrname;//13791436023
                data.password = userpass;
                MD5 mD5 = MD5.Create();
                byte[] pas = mD5.ComputeHash(Encoding.ASCII.GetBytes(data.password));
                string password = string.Empty;
                foreach (byte b in pas)
                {
                    //得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 
                    //但是在和对方测试过程中，发现我这边的MD5加密编码，经常出现少一位或几位的问题；
                    //后来分析发现是 字符串格式符的问题， X 表示大写， x 表示小写， 
                    //X2和x2表示不省略首位为0的十六进制数字；
                    password += b.ToString("x2");
                }
                data.password = password;
                string json = JsonConvert.SerializeObject(data);
                //登录的url
                string url = "http://sports.lifesense.com/sessions_service/login?systemType=2&version=4.6.7&USERAGENT=Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.109 Safari/537.36";
                string js = Post_lexin(url, json, "");
                if (int.Parse(js.Substring(js.IndexOf("code") + 6, (js.IndexOf("msg") - js.IndexOf("code")) - 8)) == 200)
                {
                    if (sle_time_plan == null)
                    {
                        sql = "INSERT INTO time_plan" +
                            "(time" +
                            ",usr_name" +
                            ",usr_password" +
                            ",maxstep" +
                            ",minstep)" +
                            "VALUES" +
                            "('" + time + "'" +
                            ",'" + usrname + "'" +
                            " ,'" + userpass + "'" +
                            ",'" + max_step + "'" +
                            ",'" + min_step + "')";
                    }
                    else
                    {
                        sql = "UPDATE time_plan " +
                                "SET usr_name = '" + usrname + "'" +
                                    ",usr_password ='" + userpass + "'" +
                                    " ,maxstep = '" + max_step + "'" +
                                    ",minstep ='" + min_step + "'" +
                                    ",time = '" + time + "'" +
                                "WHERE id=" + sle_time_plan[0].id;
                    }

                    int row = ExecuteSql(sql);
                    if (row == 1)
                    {
                        Get_plan();
                        return Json(new { msg = "新增计划成功" });
                    }
                    else
                    {
                        return Json(new { msg = "新增计划失败" });
                    }
                }
                else
                {
                    return Json(new { msg = "账号密码有误" });
                }
            }
            catch (Exception ex)
            {

                return Json(new { msg = ex.ToString() });
            }
            

        }

        #region 内部方法
        /// <summary>
        /// 获取每日计划
        /// </summary>
        static void Get_plan()
        {
            string sql = "SELECT time ,usr_name , usr_password,maxstep,minstep FROM time_plan group by time,usr_password,usr_name,maxstep,minstep order by time";
            DataSet ds = Query(sql);
            All_time_plan = DataSetToEntityList<time_plan>(ds, 0);
        }
        public static void Work_thread()
        {
            try
            {
                if (All_time_plan.Count == 0)
                {
                    //没有计划，重新获取
                    Get_plan();
                }
                string now_time = string.Empty;
                List<time_plan> Child_TimePlan = null;
                while (true)
                {
                    now_time = DateTime.Now.Hour.ToString("D2") + ":" + DateTime.Now.Minute.ToString("D2");
                    if (All_time_plan != null)
                    {
                        Child_TimePlan = All_time_plan.Where(a => string.Equals(a.time, now_time)).ToList();
                        foreach (time_plan Plan in Child_TimePlan)
                        {
                            //以Plan为参数，将GetKhlist方法加入线程池排队
                            ThreadPool.QueueUserWorkItem(up_step, Plan);

                        }
                    }

                    Thread.Sleep(new TimeSpan(0, 0, 30));
                }

            }
            catch (Exception)
            {
                
                throw;
            }
           
        }
        static long get_second()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds * 1000);
        }
        static string Post_lexin(string url, string data, string cookie)
        {
            try
            {
                WebRequest req = System.Net.WebRequest.Create(url);
                req.Method = "Post";
                //req.Headers.Add(HttpRequestHeader.ContentType, "application / json");
                //req.Headers.Add(HttpRequestHeader.ContentEncoding, "utf-8");
                req.ContentType = "application/json";
                if (cookie.Length > 0)
                {
                    req.Headers.Add(HttpRequestHeader.Cookie, cookie);
                }
                byte[] by = Encoding.ASCII.GetBytes(data);
                req.ContentLength = by.Length;
                var va = req.GetRequestStream();
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(by, 0, by.Length);
                }
                var wr = req.GetResponse();

                StreamReader reader = new StreamReader(wr.GetResponseStream(), Encoding.UTF8);
                string js = reader.ReadToEnd();
                return js;
            }
            catch (Exception ex)
            {

                return ex.Message;
            }

        }
        private static void up_step(object obj)//刷新
        {
            try
            {

                login data = new login();
                time_plan time_plan = (time_plan)obj;
                data.loginName = time_plan.usr_name;//13791436023
                data.password = time_plan.usr_password;
                Random ra = new Random();
                int up_step = ra.Next(time_plan.minstep, time_plan.maxstep);
                MD5 mD5 = MD5.Create();
                byte[] pas = mD5.ComputeHash(Encoding.ASCII.GetBytes(data.password));
                string password = string.Empty;
                foreach (byte b in pas)
                {
                    //得到的字符串使用十六进制类型格式。格式后的字符是小写的字母，如果使用大写（X）则格式后的字符是大写字符 
                    //但是在和对方测试过程中，发现我这边的MD5加密编码，经常出现少一位或几位的问题；
                    //后来分析发现是 字符串格式符的问题， X 表示大写， x 表示小写， 
                    //X2和x2表示不省略首位为0的十六进制数字；
                    password += b.ToString("x2");
                }
                data.password = password;
                string json = JsonConvert.SerializeObject(data);
                //登录的url
                string url = "http://sports.lifesense.com/sessions_service/login?systemType=2&version=4.6.7&USERAGENT=Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.109 Safari/537.36";
                string js = Post_lexin(url, json, "");
                if (int.Parse(js.Substring(js.IndexOf("code") + 6, (js.IndexOf("msg") - js.IndexOf("code")) - 8)) == 200)
                {
                    string userId = js.Substring(js.IndexOf("userId") + 9, (js.IndexOf("accessToken") - js.IndexOf("userId")) - 12);
                    string accessToken = "accessToken=" + js.Substring(js.IndexOf("accessToken") + 14, (js.IndexOf("expireAt") - js.IndexOf("accessToken")) - 17);
                    //更新所使用的url
                    url = "http://sports.lifesense.com/sport_service/sport/sport/uploadMobileStepV2?version=4.5&systemType=2&USERAGENT=Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.109 Safari/537.36";


                    string data_upta = "{\"list\":[{\"DataSource\":2,\"active\":1,\"calories\":" + (up_step / 4).ToString() + ",\"dataSource\":2,\"deviceId\":\"M_NULL\",\"distance\":" + (up_step / 3).ToString() + ",\"exerciseTime\":0,\"isUpload\":0,\"measurementTime\":\"" + DateTime.Now.ToString("yyyy-M-d H:mm:ss") + "\",\"priority\":0,\"step\":" + up_step.ToString() + ",\"type\":2,\"updated\":" + get_second().ToString() + ",\"userId\":" + userId + "}]}";

                    Post_lexin(url, data_upta, accessToken);
                }

            }
            catch (Exception ex)
            {


            }

        }
        #endregion 

        #region 基础支持方法
        /// <summary>
        /// Ds转换实体类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="p_DataSet"></param>
        /// <param name="p_TableIndex"></param>
        /// <returns></returns>
        static IList<T> DataSetToEntityList<T>(DataSet p_DataSet, int p_TableIndex)
        {
            if (p_DataSet == null || p_DataSet.Tables.Count < 0)
                return default(IList<T>);
            if (p_TableIndex > p_DataSet.Tables.Count - 1)
                return default(IList<T>);
            if (p_TableIndex < 0)
                p_TableIndex = 0;
            if (p_DataSet.Tables[p_TableIndex].Rows.Count <= 0)
                return default(IList<T>);

            DataTable p_Data = p_DataSet.Tables[p_TableIndex];
            // 返回值初始化
            IList<T> result = new List<T>();
            for (int j = 0; j < p_Data.Rows.Count; j++)
            {
                T _t = (T)Activator.CreateInstance(typeof(T));
                PropertyInfo[] propertys = _t.GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    if (p_Data.Columns.IndexOf(pi.Name.ToUpper()) != -1 && p_Data.Rows[j][pi.Name.ToUpper()] != DBNull.Value)
                    {
                        pi.SetValue(_t, p_Data.Rows[j][pi.Name.ToUpper()], null);
                    }
                    else
                    {
                        pi.SetValue(_t, null, null);
                    }
                }
                result.Add(_t);
            }
            return result;
        }
        /// <summary>
        /// 查询方法，返回ds
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        static DataSet Query(string SQLString)
        {

            DataSet ds = new DataSet();
            try
            {
                connection.Open();
                SqlDataAdapter command = new SqlDataAdapter(SQLString, connection);
                command.Fill(ds, "ds");
                connection.Close();
            }
            catch (System.Data.SqlClient.SqlException ex)
            {

                throw new Exception(ex.Message);
            }
            return ds;

        }
        /// <summary>
        /// 返回查询记录
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        static object GetSingle(string SQLString)
        {
            using (SqlCommand cmd = new SqlCommand(SQLString, connection))
            {
                try
                {
                    connection.Open();
                    object obj = cmd.ExecuteScalar();
                    connection.Close();
                    if ((Object.Equals(obj, null)) || (Object.Equals(obj, System.DBNull.Value)))
                    {
                        return null;
                    }
                    else
                    {
                        return obj;
                    }
                }
                catch (System.Data.SqlClient.SqlException e)
                {
                    connection.Close();
                    throw new Exception(e.Message);
                }
            }

        }
        /// <summary>
        /// 执行插入语句
        /// </summary>
        /// <param name="SQLString"></param>
        /// <returns></returns>
        static int ExecuteSql(string SQLString)
        {

            using (SqlCommand cmd = new SqlCommand(SQLString, connection))
            {
                try
                {
                    connection.Open();
                    int rows = cmd.ExecuteNonQuery();
                    connection.Close();
                    return rows;
                }
                catch (System.Data.SqlClient.SqlException E)
                {
                    connection.Close();
                    throw new Exception(E.Message);
                }
            }

        }
        #endregion

    }
}
    
    