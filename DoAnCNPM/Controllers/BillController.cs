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

            var billListVM = new List<BillListVM>();

            foreach (var bill in rawBills)
            {
                billListVM.Add(new BillListVM
                {
                    BillID = bill.BillID,
                    CreatedTimeFormatted = (bill.CreatedTime?.ToString("dddd, dd/MM/yyyy") ?? "N/A") + "<br />" + (bill.CreatedTime?.ToString("HH:mm") + " giờ" ?? "N/A"),
                    StaffName = bill.C_User_?.FullName ?? "N/A",
                    Payment = bill.Payment ?? "Chưa thanh toán",
                    TotalFormatted = (bill.Total ?? 0).ToString("N0") + " đ",
                    TotalFinalFormatted = (bill.Total ?? 0).ToString("N0") + " đ"
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
