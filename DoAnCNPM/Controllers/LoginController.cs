using System;
using System.Collections.Generic;
using System.Data;
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
        
        // GET: Account/Login
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
                    && u.Password == model.Password);
                if (user != null)
                {
                    Session["Username"] = user.UserName;
                    Session["UserRole"] = user.Role;

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
        public ActionResult ProfileInfo()
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Login");

            var username = Session["Username"].ToString();
            var user = db.C_User_.SingleOrDefault(u => u.UserName == username);

            if (user == null)
                return RedirectToAction("Menu", "Menu");

            var model = new ProfileInfoVM
            {
                Username = user.UserName,
                FullName = user.FullName,
                Phone = user.Phone,
                Email = user.Email,
                Role = user.Role
            };

            return View(model);
        }
    }
}