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
                    Payment = bill.Payment ?? "Chưa thanh toán",
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
