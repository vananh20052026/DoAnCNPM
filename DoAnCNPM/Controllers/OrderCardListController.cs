using QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace QuanAn.Controllers
{
    [Authorize]
    public class OrderCardListController : Controller
    {
        private QLQuanAnEntities db = new QLQuanAnEntities();

        public ActionResult OrderCardList(string statusFilter, int page = 1, int pageSize = 10)
        {
            var orders = from od in db.C_Order_Detail_
                         join fi in db.C_Food_Info_ on od.FoodID equals fi.FoodID
                         select new OrderItemVM
                         {
                             OrderID = od.OrderID,
                             FoodID = od.FoodID,
                             FoodName = fi.FoodName,
                             Quantity = (int)od.Quantity,
                             Status = od.Status
                         };

            if (!string.IsNullOrEmpty(statusFilter))
            {
                orders = orders.Where(x => x.Status == statusFilter);
            }

            // Custom sort
            List<string> customStatusOrder = new List<string> { "Chưa làm", "Đang xử lý", "Hoàn tất" };
            orders = orders.AsEnumerable()
                           .OrderBy(o => customStatusOrder.IndexOf(o.Status))
                           .ThenBy(o => o.OrderID)
                           .ThenBy(o => o.FoodName)
                           .AsQueryable();

            // Tổng số bản ghi
            var totalItems = orders.Count();

            // Lấy dữ liệu trang hiện tại
            var pagedOrders = orders.Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();

            // Truyền thông tin phân trang sang View
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.CurrentPage = page;
            ViewBag.StatusFilter = statusFilter;

            return View(pagedOrders);
        }

        [HttpPost]
        public ActionResult ChangeStatus(string orderId, string foodId, string status)
        {
            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(foodId) || string.IsNullOrEmpty(status))
            {
                TempData["ErrorMessage"] = "Mã đơn hàng, Mã món ăn hoặc Trạng thái không được để trống.";
                return RedirectToAction("OrderCardList");
            }

            var orderDetail = db.C_Order_Detail_
                                .Include(od => od.C_Food_Info_)
                                .FirstOrDefault(od => od.OrderID == orderId && od.FoodID == foodId);

            if (orderDetail == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy chi tiết món ăn với Đơn hàng {orderId} và Món ăn {foodId}.";
                return RedirectToAction("OrderCardList");
            }

            orderDetail.Status = status;

            var allDetails = db.C_Order_Detail_.Where(od => od.OrderID == orderId).ToList();

            var order = db.C_Order_.FirstOrDefault(o => o.OrderID == orderId);

            if (order != null)
            {
                if (allDetails.All(d => d.Status == "Hoàn tất"))
                {
                    order.Status = "Hoàn tất";
                }
                else if (allDetails.Any(d => d.Status == "Đang xử lý" || d.Status == "Hoàn tất"))
                {
                    order.Status = "Đang xử lý";
                }
                else
                {
                    order.Status = "Chưa làm";
                }
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái món '{orderDetail.C_Food_Info_.FoodName}' trong đơn hàng {orderId} thành '{status}'.";

            return RedirectToAction("OrderCardList");
        }
    }
}