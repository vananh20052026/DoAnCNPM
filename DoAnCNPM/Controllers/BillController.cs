using QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using System.Security.Principal;

namespace QuanAn.Controllers
{
    [Authorize]
    public class BillController : Controller
    {
        private QLQuanAnEntities db = new QLQuanAnEntities();

        [Authorize] // Ensure only authenticated users can access the Bill page
        public ActionResult Bill(int page = 1, int pageSize = 6, string paymentFilter = "", string dateFilter = "")
        {
            // Check if user is authenticated
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập để xem danh sách hóa đơn.";
                return RedirectToAction("Login", "Account");
            }

            // Get current user info for display
            string currentUserName = User.Identity.Name;
            var currentUser = db.C_User_.FirstOrDefault(u => u.UserName.Trim().ToLower() == currentUserName.ToLower());
            ViewBag.CurrentUser = currentUser?.FullName ?? currentUserName;

            // Start with the base query
            var billQuery = db.C_Bill_.Include(b => b.C_User_).AsQueryable();

            // Apply payment method filter
            if (!string.IsNullOrEmpty(paymentFilter))
            {
                billQuery = billQuery.Where(b => b.Payment == paymentFilter);
            }

            // Apply date filter
            if (!string.IsNullOrEmpty(dateFilter))
            {
                if (DateTime.TryParse(dateFilter, out DateTime filterDate))
                {
                    billQuery = billQuery.Where(b =>
                        b.CreatedTime.HasValue &&
                        DbFunctions.TruncateTime(b.CreatedTime.Value) == filterDate.Date
                    );
                }
            }

            // Get total count first (after filters but before pagination)
            var totalBillsCount = billQuery.Count();

            // Get paginated bills
            var rawBills = billQuery
                         .OrderByDescending(b => b.CreatedTime)
                         .Skip((page - 1) * pageSize)
                         .Take(pageSize)
                         .ToList();

            var allBillIDsInRawBills = rawBills.Select(b => b.BillID).ToList();
            var allBillDetails = db.C_Bill_Detail_
                                   .Where(bd => allBillIDsInRawBills.Contains(bd.BillID))
                                   .ToList();

            var allOrderIdsInBillDetails = allBillDetails.Select(bd => bd.OrderID).Distinct().ToList();

            var allOrderDetails = new List<C_Order_Detail_>();
            if (allOrderIdsInBillDetails.Any())
            {
                allOrderDetails = db.C_Order_Detail_
                                     .Include(od => od.C_Food_Info_)
                                     .Where(od => allOrderIdsInBillDetails.Contains(od.OrderID))
                                     .ToList();
            }

            var billListVM = new List<BillListVM>();
            decimal vatRate = 0.08m;

            foreach (var bill in rawBills)
            {
                decimal totalBeforeDiscount = bill.Total ?? 0;
                decimal discount = bill.Discount ?? 0;
                decimal totalFinalAfterDiscount = totalBeforeDiscount - discount;

                decimal vatAmountCalculated = totalFinalAfterDiscount * vatRate;
                decimal totalFinalWithVATCalculated = totalFinalAfterDiscount + vatAmountCalculated;

                var relatedOrderIds = allBillDetails
                                         .Where(bd => bd.BillID == bill.BillID)
                                         .Select(bd => bd.OrderID)
                                         .Where(id => id != null)
                                         .Distinct()
                                         .ToList();

                if (!relatedOrderIds.Any() && !string.IsNullOrEmpty(bill.OrderID))
                {
                    relatedOrderIds.Add(bill.OrderID);
                }

                string relatedOrderIdsFormatted = relatedOrderIds.Any() ?
                                                  string.Join(", ", relatedOrderIds.OrderBy(id => id)) :
                                                  "N/A";

                var foodItemsForCurrentBill = allOrderDetails
                                              .Where(od => relatedOrderIds.Contains(od.OrderID))
                                              .ToList();

                var currentBillAggregatedItems = foodItemsForCurrentBill
                    .GroupBy(od => new { od.FoodID, od.C_Food_Info_.FoodName, od.UnitPrice })
                    .Select(g => new AggregatedFoodItemVM
                    {
                        FoodName = g.Key.FoodName,
                        TotalQuantity = g.Sum(od => od.Quantity ?? 0),
                        UnitPrice = g.Key.UnitPrice ?? 0,
                        TotalPrice = g.Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0))
                    })
                    .OrderBy(item => item.FoodName)
                    .ToList();

                billListVM.Add(new BillListVM
                {
                    BillID = bill.BillID,
                    CreatedTimeFormatted = (bill.CreatedTime?.ToString("dddd, dd/MM/yyyy") ?? "N/A") + "<br />" + (bill.CreatedTime?.ToString("HH:mm") + " giờ" ?? "N/A"),
                    StaffName = bill.C_User_?.FullName ?? "N/A",

                    // Update payment display logic
                    Payment = GetPaymentDisplayStatus(bill, null),

                    TotalFormatted = totalBeforeDiscount.ToString("N0") + " đ",
                    DiscountFormatted = (discount > 0 ? "- " + discount.ToString("N0") : "0") + " đ",
                    TotalFinalFormatted = totalFinalAfterDiscount.ToString("N0") + " đ",

                    VATAmountFormatted = vatAmountCalculated.ToString("N0") + " đ",
                    TotalFinalWithVATFormatted = totalFinalWithVATCalculated.ToString("N0") + " đ",

                    RelatedOrderIDs = relatedOrderIds,
                    RelatedOrderIDsFormatted = relatedOrderIdsFormatted,
                    AggregatedFoodItems = currentBillAggregatedItems
                });
            }

            // Get distinct payment methods for filter dropdown
            ViewBag.PaymentMethods = db.C_Bill_
                .Where(b => !string.IsNullOrEmpty(b.Payment))
                .Select(b => b.Payment)
                .Distinct()
                .ToList();

            ViewBag.PaymentFilter = paymentFilter;
            ViewBag.DateFilter = dateFilter;

            var viewModel = new BillVM
            {
                Bills = billListVM,
                CurrentPage = page,
                PageSize = pageSize,
                TotalBills = totalBillsCount,
                TotalPages = (int)Math.Ceiling((double)totalBillsCount / pageSize)
            };

            return View(viewModel);
        }

        [HttpPost]
        [Authorize]
        public ActionResult PayBill(string orderId, string paymentMethod = "Tiền mặt", decimal discountPercent = 0)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["ErrorMessage"] = "Bạn phải đăng nhập để thực hiện thanh toán.";
                return RedirectToAction("Login", "Account");
            }

            string currentUserName = User.Identity.Name?.Trim();
            if (string.IsNullOrEmpty(currentUserName))
            {
                TempData["ErrorMessage"] = "Lỗi: Không tìm thấy thông tin người dùng.";
                return RedirectToAction("Login", "Account");
            }

            var user = db.C_User_.FirstOrDefault(u => u.UserName.Trim().ToLower() == currentUserName.ToLower());
            if (user == null)
            {
                TempData["ErrorMessage"] = "Tài khoản không tồn tại trong hệ thống.";
                return RedirectToAction("Login", "Account");
            }

            string userId = user.UserID.Trim();
            string userDisplayName = user.FullName ?? user.UserName;

            var order = db.C_Order_.Include(o => o.C_Table_).FirstOrDefault(o => o.OrderID == orderId);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Bill");
            }

            if (order.Status != "Hoàn tất" && order.Status != "Đã tạo bill")
            {
                TempData["ErrorMessage"] = $"Chỉ thanh toán đơn 'Hoàn tất' hoặc 'Đã tạo bill'. Trạng thái hiện tại: {order.Status}";
                return RedirectToAction("Bill");
            }

            if (order.Status == "Đã thanh toán")
            {
                TempData["ErrorMessage"] = $"Đơn hàng {orderId} đã được thanh toán.";
                return RedirectToAction("Bill");
            }

            // 🔍 Tìm BillID qua bảng Bill_Detail (nếu đã tạo bill cho đơn gộp)
            string existingBillId = db.C_Bill_Detail_
                .Where(bd => bd.OrderID == orderId)
                .Select(bd => bd.BillID)
                .FirstOrDefault();

            var existingBill = db.C_Bill_.FirstOrDefault(b => b.BillID == existingBillId);

            var relatedOrderIds = db.C_Bill_Detail_
                .Where(bd => bd.BillID == existingBillId)
                .Select(bd => bd.OrderID)
                .ToList();

            var allOrderDetails = db.C_Order_Detail_
                .Where(od => relatedOrderIds.Contains(od.OrderID))
                .ToList();

            if (!allOrderDetails.Any())
            {
                TempData["ErrorMessage"] = "Đơn hàng không có món để thanh toán.";
                return RedirectToAction("Bill");
            }

            decimal tongTienHang = allOrderDetails.Sum(od => (od.Quantity ?? 0) * (od.UnitPrice ?? 0));
            decimal giamGiaGoc = relatedOrderIds.Sum(id => db.C_Order_.Where(o => o.OrderID == id).Select(o => o.Discount ?? 0).FirstOrDefault());
            decimal giamGiaThem = tongTienHang * (discountPercent / 100);
            decimal tongGiamGia = giamGiaGoc + giamGiaThem;
            decimal tongTienSauGiam = tongTienHang - tongGiamGia;

            if (existingBill != null && order.Status == "Đã tạo bill")
            {
                existingBill.Payment = paymentMethod;
                existingBill.Discount = tongGiamGia;
                existingBill.TotalFinal = tongTienSauGiam;
                existingBill.UserID = userId;

                foreach (var orderIdInBill in relatedOrderIds)
                {
                    var o = db.C_Order_.FirstOrDefault(x => x.OrderID == orderIdInBill);
                    if (o != null)
                    {
                        o.Status = "Đã thanh toán";
                        o.Discount = db.C_Order_Detail_
                            .Where(d => d.OrderID == o.OrderID)
                            .Sum(d => (d.Quantity ?? 0) * (d.UnitPrice ?? 0)) * (discountPercent / 100);

                        if (o.C_Table_ != null)
                            o.C_Table_.Status = "Available";
                    }
                }

                try
                {
                    db.SaveChanges();
                    TempData["SuccessMessage"] = $"Thanh toán thành công! Người xử lý: {userDisplayName}";
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "Lỗi khi thanh toán: " + ex.Message;
                }

                return RedirectToAction("Bill");
            }

            if (existingBill != null)
            {
                TempData["ErrorMessage"] = "Đơn đã được thanh toán hoặc đã gộp vào bill.";
                return RedirectToAction("Bill");
            }

            // ❗ Trường hợp tạo bill mới cho đơn chưa gộp
            string newBillId = Guid.NewGuid().ToString("N").Substring(0, 10);
            while (db.C_Bill_.Any(b => b.BillID == newBillId))
            {
                newBillId = Guid.NewGuid().ToString("N").Substring(0, 10);
            }

            var newBill = new C_Bill_
            {
                BillID = newBillId,
                OrderID = order.OrderID,
                UserID = userId,
                CreatedTime = DateTime.Now,
                Total = tongTienHang,
                Discount = tongGiamGia,
                TotalFinal = tongTienSauGiam,
                Payment = paymentMethod
            };
            db.C_Bill_.Add(newBill);

            var newDetail = new C_Bill_Detail_
            {
                BillID = newBillId,
                OrderID = order.OrderID,
                Quantity = allOrderDetails.Sum(od => od.Quantity ?? 0),
                UnitPrice = tongTienHang
            };
            db.C_Bill_Detail_.Add(newDetail);

            order.Status = "Đã thanh toán";
            order.Discount = tongGiamGia;
            if (order.C_Table_ != null)
                order.C_Table_.Status = "Available";

            try
            {
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Thanh toán thành công! Người xử lý: {userDisplayName}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi khi lưu hóa đơn: " + ex.Message;
            }

            return RedirectToAction("Bill");
        }


        // Update the GetPaymentDisplayStatus method to only use Payment field
        private string GetPaymentDisplayStatus(C_Bill_ bill, string orderStatus)
        {
            // Since we're not using bill.Status anymore, just return the payment method
            return bill.Payment ?? "Chưa thanh toán";
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
