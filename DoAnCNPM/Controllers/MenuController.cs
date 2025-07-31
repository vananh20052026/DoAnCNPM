using QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Data.Entity.Validation;

namespace QuanAn.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private QLQuanAnEntities db = new QLQuanAnEntities();

        private string GenerateNewFoodId()
        {
            string newId = "";
            bool isUnique = false;
            int maxAttempts = 100;
            int attempts = 0;

            while (!isUnique && attempts < maxAttempts)
            {
                string guidPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                newId = $"FD{guidPart}";

                isUnique = !db.C_Food_Info_.Any(o => o.FoodID.Trim() == newId.Trim());

                attempts++;
            }

            if (!isUnique)
            {
                throw new Exception("Không thể tạo FoodID duy nhất sau nhiều lần thử.");
            }

            return newId;
        }

        private string GenerateNewCategoryId()
        {
            var lastCategory = db.C_Category_
                .OrderByDescending(c => c.CateID)
                .FirstOrDefault();

            int nextId = 1;

            if (lastCategory != null && int.TryParse(lastCategory.CateID.Trim(), out int parsedId))
            {
                nextId = parsedId + 1;
            }

            return nextId.ToString(); // VD: "8"
        }

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

        [HttpPost]
        public ActionResult EditFood(string id, string foodName, decimal unitPrice, string foodImageUrl)
        {
            var food = db.C_Food_Info_.FirstOrDefault(f => f.FoodID.Trim() == id.Trim());
            if (food != null)
            {
                food.FoodName = foodName;
                food.UnitPrice = unitPrice;

                // ✅ Cập nhật ảnh mới nếu có link
                if (!string.IsNullOrEmpty(foodImageUrl))
                {
                    food.FoodImage = foodImageUrl.Trim();
                }

                db.SaveChanges();
            }

            return RedirectToAction("FoodDetail", new { id = id.Trim() });
        }

        [Route("Menu/FoodDetail/{id}")]
        public ActionResult FoodDetail(string id, string orderId, string tableName)
        {
            if (string.IsNullOrEmpty(id))
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }

            var food = db.C_Food_Info_.FirstOrDefault(f => f.FoodID.Trim() == id.Trim());
            if (food == null)
            {
                return HttpNotFound();
            }

            var recipe = db.C_Recipe_.FirstOrDefault(r => r.FoodID.Trim() == id.Trim());
            var ingredients = new List<FoodDetailVM.IngredientDetail>();

            if (recipe != null)
            {
                ingredients = db.C_Recipe_Detail_
                    .Where(rd => rd.RecipeID == recipe.RecipeID)
                    .Select(rd => new FoodDetailVM.IngredientDetail
                    {
                        IngreName = rd.C_Ingredient_ != null ? rd.C_Ingredient_.IngreName : "N/A",
                        Quantity = (double)(rd.Quantity ?? 0),
                        UnitMeasurement = rd.UnitMeasurement
                    }).ToList();
            }

            var viewModel = new FoodDetailVM
            {
                UnitPrice = food.UnitPrice ?? 0,
                Food = food,
                Ingredients = ingredients
            };

            return View("~/Views/FoodDetail/FoodDetail.cshtml", viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddFood(string FoodName, decimal UnitPrice, string CategoryID, string NewCategoryName, string FoodImageUrl)
        {
            if (string.IsNullOrEmpty(FoodImageUrl))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập link ảnh món ăn.";
                return RedirectToAction("Menu");
            }

            try
            {
                // Bước 1: Xử lý Category
                string finalCateID = CategoryID?.Trim();

                if (!string.IsNullOrEmpty(NewCategoryName))
                {
                    finalCateID = GenerateNewCategoryId(); // đảm bảo <= 10 ký tự

                    var newCategory = new C_Category_
                    {
                        CateID = finalCateID,
                        CateName = NewCategoryName.Trim()
                    };

                    db.C_Category_.Add(newCategory);
                    db.SaveChanges(); // lưu danh mục trước
                }

                // Bước 2: Tạo món ăn mới
                var newFoodID = GenerateNewFoodId();

                var newFood = new C_Food_Info_
                {
                    FoodID = newFoodID,
                    FoodName = FoodName,
                    UnitPrice = UnitPrice,
                    FoodImage = FoodImageUrl.Trim(), // 💥 dùng URL người dùng nhập
                    CateID = finalCateID
                };

                db.C_Food_Info_.Add(newFood);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Đã thêm món mới thành công.";
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                string errorDetails = string.Join("; ", dbEx.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors.Select(v => $"{v.PropertyName}: {v.ErrorMessage}")));

                TempData["ErrorMessage"] = "Thêm món thất bại! " + errorDetails;
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return RedirectToAction("Menu");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteFood(string id)
        {
            try
            {
                var food = db.C_Food_Info_.FirstOrDefault(f => f.FoodID == id);
                if (food != null)
                {
                    db.C_Food_Info_.Remove(food);
                    db.SaveChanges();
                    TempData["SuccessMessage"] = "Xóa món thành công.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy món để xóa.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Đã xảy ra lỗi: " + ex.Message;
            }

            return RedirectToAction("Menu");
        }

        public ActionResult FoodList(string searchTerm, int? page)
        {
            var model = new MenuProductVM();
            var foods = db.C_Food_Info_.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                foods = foods.Where(f => f.FoodName.Contains(searchTerm)
                                         || (f.Description != null && f.Description.Contains(searchTerm))
                                         || (f.C_Category_ != null && f.C_Category_.CateName.Contains(searchTerm)));
            }

            int pageNumber = page ?? 1;
            int pageSize = 6;

            model.itemFoods = foods
                                     .OrderBy(f => f.C_Order_Detail_.Count())
                                     .ToPagedList(pageNumber, pageSize)
                                     .ToList();

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
