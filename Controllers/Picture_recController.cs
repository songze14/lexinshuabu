using System.Drawing;
using System.Web;
using System.Web.Mvc;

namespace weixinshuabu.Controllers
{
    public class Picture_recController : Controller
    {
        // GET: Picture_rec
        public ActionResult Index()
        {
            return View();
        }
        public  void Image_pro(HttpPostedFileBase fileResult )
        {
            Bitmap bitmap = new Bitmap(fileResult.InputStream);
            
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color img_col = bitmap.GetPixel(i, j);
                    int grav_col =  (int)(img_col.R * 0.299 + img_col.G * 0.587 + img_col.B * 0.114);
                    bitmap.SetPixel(i, j, Color.FromArgb(grav_col, grav_col, grav_col));
                }
            }
            for (int i = 0; i < bitmap.Width; i++)
            {
                for (int j = 0; j < bitmap.Height; j++)
                {
                    Color img_col = bitmap.GetPixel(i, j);
                    if (img_col.R!=0 || img_col.G!=0||img_col.B!=0)
                    {
                        bitmap.SetPixel(i, j, Color.FromArgb(255,255,255));
                    }
                   
                }
            }
            bitmap.Save("C:\\"+ fileResult.FileName+ ".jpg");
        }

    }
}