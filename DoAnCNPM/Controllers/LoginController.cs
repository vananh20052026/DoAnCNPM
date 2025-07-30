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

        // POST: Account/ProfileInfo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProfileInfo(ProfileInfoVM model)
        {
            if (Session["Username"] == null)
                return RedirectToAction("Login", "Login");

            var username = Session["Username"].ToString();
            var user = db.C_User_.SingleOrDefault(u => u.UserName == username);

            if (user == null)
                return RedirectToAction("Menu", "Menu");

            if (ModelState.IsValid)
            {
                try
                {
                    user.FullName = model.FullName;

                    if (model.Phone.HasValue)
                    {
                        if (model.Phone.Value > int.MaxValue)
                        {
                            ModelState.AddModelError("Phone", "Số điện thoại vượt quá độ dài cho phép.");
                            model.Role = user.Role; // Thêm dòng này
                            return View(model);
                        }
                        user.Phone = (int)model.Phone.Value;
                    }

                    user.Email = model.Email;

                    // Đổi mật khẩu nếu có
                    if (!string.IsNullOrWhiteSpace(model.Password))
                    {
                        if (string.IsNullOrWhiteSpace(model.CurrentPassword))
                        {
                            ModelState.AddModelError("CurrentPassword", "Vui lòng nhập mật khẩu hiện tại.");
                            model.Role = user.Role;
                            return View(model);
                        }

                        TempData["Debug"] = "CurrentPassword: " + model.CurrentPassword + " | DB Password: " + user.Password;

                        if (model.CurrentPassword != user.Password)
                        {
                            ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                            model.Role = user.Role;
                            return View(model);
                        }

                        if (model.Password != model.ConfirmPassword)
                        {
                            ModelState.AddModelError("ConfirmPassword", "Mật khẩu xác nhận không khớp.");
                            model.Role = user.Role;
                            return View(model);
                        }

                        user.Password = model.Password;
                        db.SaveChanges();
                        TempData["SuccessMessage"] = "Đổi mật khẩu thành công. Vui lòng đăng nhập lại!";
                        Session.Clear();
                        return RedirectToAction("Login", "Login");
                    }

                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction("ProfileInfo");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    TempData["Debug"] = ex.ToString(); // thêm dòng này
                    ModelState.AddModelError("", "Lỗi khi lưu: " + ex.Message);
                }
            }

            model.Role = user.Role; // Cần dòng này trước khi return View nếu có lỗi
            return View(model);
        }
    }
}