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


    }
}