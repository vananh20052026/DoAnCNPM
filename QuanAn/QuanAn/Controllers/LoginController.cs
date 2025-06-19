using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Web.UI.WebControls;
using QuanAn.Models;
using QuanAn.Models.ViewModel;

namespace QuanAn.Controllers
{
    public class LoginController : Controller
    {
        QLQuanAnEntities db = new QLQuanAnEntities();
        // POST: Account/Login
        public ActionResult Login()
        {
            return View();

        }
        // POST: Account/Login

        [HttpPost]

        [ValidateAntiForgeryToken]

        public ActionResult Login(LoginVM model)
        {
            if (ModelState.IsValid)
            {
                var user = db.C_User_.SingleOrDefault(u => u.UserName == model.Username
                    && u.Password == model.Password
                    && u.Role == "NV");
                if (user != null)
                {
                    //Lưu trạng thái đăng nhập vào session
                    Session["Username"] = user.UserName;
                    Session["UserRole"] = user.Role;

                    //Lưu thông tin xác thực người dùng vào cookie
                    FormsAuthentication.SetAuthCookie(user.UserName, false);

                    return RedirectToAction("Menu", "Menu");
                }
                else
                {
                    ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                }
            }
            return View(model);
        }

        //GET: Account/Logout
        public ActionResult Logout()
        {
            Session.Clear();
            return RedirectToAction("Login", "Login");
        }

        //GET: Account/ProfileInfo
        [Authorize]
        public ActionResult ProfileInfo()
        {
            var user = db.C_User_.SingleOrDefault(u => u.UserName == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Menu", "Menu");
            }

            var model = new ProfileInfoVM
            {
                Username = user.UserName,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
            };

            return View(model);
        }

        //POST: Account/ProfileInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public ActionResult ProfileInfo(ProfileInfoVM model)
        {
            if (ModelState.IsValid)
            {
                var user = db.C_User_.SingleOrDefault(u => u.UserName == User.Identity.Name);
                if (user == null)
                {
                    return RedirectToAction("Menu", "Menu");
                }

             

                user.UserName = model.Username;
                user.FullName = model.FullName;
                user.Phone =(int?)model.Phone;
                user.Email = model.Email;
                

                db.SaveChanges();

                return RedirectToAction("ProfileInfo");
            }
            return View(model);
        }
    }
}