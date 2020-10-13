using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace WebApplication1.Controllers
{

    public class weixinshuabuController : Controller
    {
         class login
        {
            public string appType = " 6";
            public string clientId = "88888";
            public string loginName { set; get; }
            public string password { set; get; }
            public string roleType = " 0";
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
            public string  userId { set; get; }
            public string accessToken { set; get; }//校验用的cooke
        }
        public ActionResult Index()
        {
            return View(); 
        }
       static long  get_second()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalSeconds * 1000);
        }
       static string Post_lexin(string url,string data,string cookie)
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
       public JsonResult up_step(string usrid,string userpass,string step)//点击事件触发的方法
        {
            try
            {
                login data = new login();
                data.loginName = usrid;//13791436023
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
                    string userId = js.Substring(js.IndexOf("userId") + 9, (js.IndexOf("accessToken") - js.IndexOf("userId")) - 12);
                    string accessToken = "accessToken=" + js.Substring(js.IndexOf("accessToken") + 14, (js.IndexOf("expireAt") - js.IndexOf("accessToken")) - 17);
                    //更新所使用的url
                    url = "http://sports.lifesense.com/sport_service/sport/sport/uploadMobileStepV2?version=4.5&systemType=2&USERAGENT=Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/48.0.2564.109 Safari/537.36";

                    int up_step = int.Parse(step);
                    string data_upta = "{\"list\":[{\"DataSource\":2,\"active\":1,\"calories\":" + (up_step / 4).ToString() + ",\"dataSource\":2,\"deviceId\":\"M_NULL\",\"distance\":" + (up_step / 3).ToString() + ",\"exerciseTime\":0,\"isUpload\":0,\"measurementTime\":\"" + DateTime.Now.ToString("yyyy-M-d H:mm:ss") + "\",\"priority\":0,\"step\":" + step + ",\"type\":2,\"updated\":" + get_second().ToString() + ",\"userId\":" + userId + "}]}";

                    return Json(Post_lexin(url, data_upta, accessToken));
                }
                return Json(new { step = 0 });
            }
            catch (Exception ex)
            {

                return Json(new { message = ex.Message });
            }
            
        }
    }
    
    
}