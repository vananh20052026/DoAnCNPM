using Newtonsoft.Json;
using PagedList;
using QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Validation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace QuanAn.Controllers
{
    [Authorize]
    public class MenuController : Controller
    {
        private QLQuanAnEntities db = new QLQuanAnEntities();

        private string GenerateNewOrderId()
        {
            string newId = "";
            bool isUnique = false;
            int maxAttempts = 100;
            int attempts = 0;

            while (!isUnique && attempts < maxAttempts)
            {
                string guidPart = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                newId = $"HD{guidPart}";

                isUnique = !db.C_Order_.Any(o => o.OrderID.Trim() == newId.Trim());

                attempts++;
            }

            if (!isUnique)
            {
                throw new Exception("Không thể tạo OrderID duy nhất sau nhiều lần thử.");
            }

            return newId;
        }

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

        public ActionResult Menu(string orderId, string tableName, string searchTerm, int? page, int? categoryCount)
        {
            var model = new MenuProductVM();
            var foods = db.C_Food_Info_.AsQueryable();
            var category = db.C_Category_.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                foods = foods.Where(f => f.FoodName.Contains(searchTerm)
                                          || (f.Description != null && f.Description.Contains(searchTerm))
                                          || (f.C_Category_ != null && f.C_Category_.CateName.Contains(searchTerm)));
            }

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateOrder(CreateOrderVM vm) // Trả về ActionResult
        {
            if (vm == null || string.IsNullOrEmpty(vm.TableName) || vm.Items == null || !vm.Items.Any())
            {
                TempData["ErrorMessage"] = "Dữ liệu đơn hàng không hợp lệ. Vui lòng thêm món và chọn bàn.";
                return RedirectToAction("Menu", new { tableName = vm?.TableName }); // Giữ lại tableName nếu có
            }

            var table = db.C_Table_.FirstOrDefault(t => t.TableName.Trim().ToLower() == vm.TableName.Trim().ToLower());
            if (table == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy bàn đã chọn. Vui lòng kiểm tra lại.";
                return RedirectToAction("Menu", new { tableName = vm.TableName });
            }

            string newOrderId;
            try
            {
                newOrderId = GenerateNewOrderId();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi tạo mã đơn hàng: " + ex.Message;
                return RedirectToAction("Menu", new { tableName = vm.TableName });
            }

            string currentUserNameFromIdentity = User.Identity.Name;
            string trimmedCurrentUserName = currentUserNameFromIdentity?.Trim();

            if (string.IsNullOrEmpty(trimmedCurrentUserName))
            {
                TempData["ErrorMessage"] = "Lỗi: Không tìm thấy thông tin người dùng đang đăng nhập. Vui lòng đăng nhập lại.";
                return RedirectToAction("Menu", new { tableName = vm.TableName });
            }

            var existingUser = db.C_User_.FirstOrDefault(u => u.UserName.Trim().ToLower() == trimmedCurrentUserName.ToLower());

            if (existingUser == null)
            {
                TempData["ErrorMessage"] = "Lỗi: Thông tin người dùng đăng nhập không tồn tại trong hệ thống. Vui lòng liên hệ quản trị viên.";
                return RedirectToAction("Menu", new { tableName = vm.TableName });
            }

            string userIdToAssign = existingUser.UserID.Trim();

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = new C_Order_
                    {
                        OrderID = newOrderId,
                        CreatedTime = DateTime.Now,
                        Status = "Chưa làm",
                        TableID = table.TableID,
                        Total = 0,
                        Discount = 0,
                        Note = null,
                        ReservationID = null,
                        UserID = userIdToAssign
                    };
                    db.C_Order_.Add(order);
                    db.SaveChanges();

                    decimal orderTotal = 0;

                    foreach (var item in vm.Items)
                    {
                        var food = db.C_Food_Info_.FirstOrDefault(f => f.FoodName.Trim().ToLower() == item.FoodName.Trim().ToLower());
                        if (food == null)
                        {
                            Debug.WriteLine($"Warning: Food '{item.FoodName}' not found. Skipping this order item.");
                            continue;
                        }

                        decimal actualUnitPrice = food.UnitPrice ?? 0;

                        var detail = new C_Order_Detail_
                        {
                            OrderID = order.OrderID,
                            FoodID = food.FoodID,
                            Quantity = item.Quantity,
                            UnitPrice = actualUnitPrice,
                            Status = "Chưa làm",
                        };
                        db.C_Order_Detail_.Add(detail);

                        orderTotal += actualUnitPrice * item.Quantity;
                    }

                    order.Total = orderTotal;
                    db.Entry(order).State = EntityState.Modified;
                    db.SaveChanges();

                    table.Status = "Occupied";
                    db.Entry(table).State = EntityState.Modified;
                    db.SaveChanges();

                    transaction.Commit();

                    TempData["SuccessMessage"] = "Đơn hàng đã được tạo thành công!";
                    return RedirectToAction("Menu", new { orderId = newOrderId, tableName = vm.TableName });
                }
                catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    transaction.Rollback();
                    var fullErrorMessage = string.Join("; ", dbEx.EntityValidationErrors.SelectMany(e => e.ValidationErrors.Select(v => $"{v.PropertyName}: {v.ErrorMessage}")));
                    Debug.WriteLine($"DbEntityValidationException in CreateOrder: {fullErrorMessage}");
                    TempData["ErrorMessage"] = "Lỗi validation khi tạo đơn hàng: " + fullErrorMessage;
                    return RedirectToAction("Menu", new { tableName = vm.TableName });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.WriteLine($"FATAL ERROR in CreateOrder: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    }
                    Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                    TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi tạo đơn hàng. Vui lòng thử lại sau.";
                    return RedirectToAction("Menu", new { tableName = vm.TableName });
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMultipleFoodsToOrder(string orderId, string foodsData, string tableName)
        {
            Debug.WriteLine($"AddMultipleFoodsToOrder received: OrderId={orderId}, FoodsData={foodsData}, TableName={tableName}");

            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(foodsData))
            {
                TempData["ErrorMessage"] = "Thiếu thông tin đơn hàng hoặc món ăn.";
                return RedirectToAction("Menu", new { orderId = orderId, tableName = tableName });
            }

            List<FoodAddVM> foodsToAdd;
            try
            {
                foodsToAdd = JsonConvert.DeserializeObject<List<FoodAddVM>>(foodsData);
                if (foodsToAdd == null || !foodsToAdd.Any())
                {
                    TempData["ErrorMessage"] = "Không có món nào được gửi để thêm vào đơn hàng.";
                    return RedirectToAction("Menu", new { orderId = orderId, tableName = tableName });
                }
            }
            catch (JsonException jEx)
            {
                TempData["ErrorMessage"] = "Lỗi định dạng dữ liệu món ăn khi thêm vào đơn: " + jEx.Message;
                Debug.WriteLine($"JSON Deserialization Error in AddMultipleFoodsToOrder: {jEx.Message}");
                return RedirectToAction("Menu", new { orderId = orderId, tableName = tableName });
            }

            try
            {
                var existingOrder = db.C_Order_.FirstOrDefault(o => o.OrderID.Trim() == orderId.Trim());
                if (existingOrder == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy đơn hàng để thêm món.";
                    return RedirectToAction("Menu", new { tableName = tableName });
                }

                decimal totalAddedPrice = 0;

                var groupedIncomingFoods = foodsToAdd.GroupBy(item => item.FoodID.Trim().ToLower())
                                                     .Select(g => new
                                                     {
                                                         FoodId = g.Key,
                                                         Quantity = g.Sum(item => item.Quantity),
                                                         UnitPrice = g.First().UnitPrice
                                                     }).ToList();

                foreach (var item in groupedIncomingFoods)
                {
                    var food = db.C_Food_Info_.FirstOrDefault(f => f.FoodID.Trim().ToLower() == item.FoodId);
                    if (food == null)
                    {
                        Debug.WriteLine($"Warning: Food with ID '{item.FoodId}' not found. Skipping this item.");
                        continue;
                    }

                    var existingDetail = db.C_Order_Detail_
                        .FirstOrDefault(od => od.OrderID.Trim() == orderId.Trim() && od.FoodID.Trim() == food.FoodID.Trim());

                    if (existingDetail != null)
                    {
                        existingDetail.Quantity += item.Quantity;
                        existingDetail.UnitPrice = food.UnitPrice ?? 0;
                    }
                    else
                    {
                        var orderDetail = new C_Order_Detail_
                        {
                            OrderID = orderId,
                            FoodID = food.FoodID,
                            Quantity = item.Quantity,
                            UnitPrice = food.UnitPrice ?? 0,
                            Status = "Chưa làm"
                        };
                        db.C_Order_Detail_.Add(orderDetail);
                    }
                    totalAddedPrice += (food.UnitPrice ?? 0) * item.Quantity;
                }

                existingOrder.Total = (existingOrder.Total ?? 0) + totalAddedPrice;

                db.SaveChanges();

                TempData["SuccessMessage"] = "Đã thêm các món vào đơn hàng thành công.";
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var fullErrorMessage = string.Join("; ", ex.EntityValidationErrors.SelectMany(e => e.ValidationErrors.Select(v => $"{v.PropertyName}: {v.ErrorMessage}")));
                Debug.WriteLine($"DbEntityValidationException in AddMultipleFoodsToOrder: {fullErrorMessage}");
                TempData["ErrorMessage"] = "Lỗi validation khi thêm món vào đơn hàng: " + fullErrorMessage;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"FATAL ERROR in AddMultipleFoodsToOrder: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Debug.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }
                Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                TempData["ErrorMessage"] = "Đã xảy ra lỗi hệ thống khi thêm món vào đơn hàng. Vui lòng thử lại sau.";
            }

            return RedirectToAction("Menu", new { orderId = orderId, tableName = tableName });
        }
    }
}