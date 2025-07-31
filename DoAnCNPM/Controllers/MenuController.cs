Menu cơ bản với tìm kiếm và lọc danh mụcusing QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;

namespace QuanAn.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private QLQuanAnEntities db = new QLQuanAnEntities();

        public ActionResult Menu(string orderId, string tableName, string searchTerm, int? page, int? categoryCount)
        {
            var model = new MenuProductVM();
            var foods = db.C_Food_Info_.AsQueryable();
            var category = db.C_Category_.AsQueryable();

            // Basic search functionality
            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                foods = foods.Where(f => f.FoodName.Contains(searchTerm)
                                          || (f.Description != null && f.Description.Contains(searchTerm))
                                          || (f.C_Category_ != null && f.C_Category_.CateName.Contains(searchTerm)));
            }

            // Category filter functionality
            if (categoryCount.HasValue && categoryCount.Value != 0)
            {
                string categoryIdAsString = categoryCount.Value.ToString();
                foods = foods.Where(f => f.CateID == categoryIdAsString);
            }

            model.itemFoods = foods.ToList();
            model.categories = category.ToList();

            ViewBag.OrderId = orderId; 
            ViewBag.TableName = tableName;
            ViewBag.CategoryCount = categoryCount ?? 0;

            ViewBag.CategoryList = db.C_Category_.ToList();

            ViewBag.CategoryTitle = categoryCount.HasValue && categoryCount.Value != 0
        ? db.C_Category_.FirstOrDefault(c => c.CateID == categoryCount.Value.ToString())?.CateName
        : "Tất cả";

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
