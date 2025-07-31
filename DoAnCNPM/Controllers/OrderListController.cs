using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanAn.Controllers
{
    public class OrderListController : Controller
    {
        private QLQuanAnEntities db = new QLQuanAnEntities();

        // GET: OrderList/OrderList
        public ActionResult OrderList(string statusFilter, string tableFilter, string dayFilter, int page = 1, int pageSize = 10)
        {
            var masterOrdersQuery = db.C_Order_
                .Include(o => o.C_Table_)
                .Include(o => o.C_User_)
                .AsQueryable();

            ViewBag.AllTables = db.C_Table_
                                   .Select(t => t.TableName)
                                   .Distinct()
                                   .OrderBy(t => t)
                                   .ToList();

            // Apply filters
            if (!string.IsNullOrEmpty(statusFilter))
            {
                masterOrdersQuery = masterOrdersQuery.Where(o => o.Status == statusFilter);
            }

            if (!string.IsNullOrEmpty(tableFilter))
            {
                masterOrdersQuery = masterOrdersQuery.Where(o => o.C_Table_.TableName == tableFilter);
            }

            if (!string.IsNullOrEmpty(dayFilter))
            {
                DateTime filterDate;
                if (DateTime.TryParseExact(dayFilter, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out filterDate))
                {
                    masterOrdersQuery = masterOrdersQuery.Where(o =>
                        o.CreatedTime.HasValue &&
                        DbFunctions.TruncateTime(o.CreatedTime.Value) == DbFunctions.TruncateTime(filterDate)
                    );
                    ViewBag.DayFilter = dayFilter;
                }
                else
                {
                    TempData["ErrorMessage"] = "Định dạng ngày không hợp lệ. Vui lòng sử dụng định dạng DD/MM/YYYY.";
                }
            }

            // Apply ordering: Chưa làm -> Đang xử lý -> Hoàn tất -> Đã tạo bill -> Đã thanh toán
            masterOrdersQuery = masterOrdersQuery
                .OrderBy(o => o.Status == "Chưa làm" ? 0
                            : o.Status == "Đang xử lý" ? 1
                            : o.Status == "Hoàn tất" ? 2
                            : o.Status == "Đã tạo bill" ? 3
                            : o.Status == "Đã thanh toán" ? 4
                            : 5)
                .ThenByDescending(o => o.CreatedTime);

            // Apply pagination
            int totalOrders = masterOrdersQuery.Count();
            var pagedOrders = masterOrdersQuery.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var orderListVM = new List<OrderListVM>();

            foreach (var masterOrder in pagedOrders)
            {
                var orderVM = new OrderListVM
                {
                    OrderID = masterOrder.OrderID,
                    CreatedTime = masterOrder.CreatedTime,
                    TableName = masterOrder.C_Table_?.TableName ?? "N/A",
                    Staff = masterOrder.C_User_?.FullName ?? "N/A",
                    Discount = masterOrder.Discount,
                    TotalFinal = masterOrder.Total,
                    Status = masterOrder.Status,
                    OrderItems = new List<OrderItemVM>()
                };

                var orderDetails = db.C_Order_Detail_
                    .Where(od => od.OrderID == masterOrder.OrderID)
                    .Join(db.C_Food_Info_,
                        od => od.FoodID,
                        fi => fi.FoodID,
                        (od, fi) => new OrderItemVM
                        {
                            OrderID = od.OrderID,
                            FoodID = od.FoodID,
                            FoodName = fi.FoodName,
                            Quantity = (int)od.Quantity,
                            UnitPrice = od.UnitPrice ?? fi.UnitPrice ?? 0,
                            Status = od.Status
                        })
                    .ToList();

                orderVM.OrderItems = orderDetails;
                orderListVM.Add(orderVM);
            }

            ViewBag.StatusFilter = statusFilter;
            ViewBag.TableFilter = tableFilter;

            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalOrders = totalOrders;
            ViewBag.DayFilter = dayFilter;

            ViewBag.MergeableOrders = db.C_Order_
                .Include(o => o.C_Table_)
                .Include(o => o.C_User_)
                .Where(o => o.Status == "Hoàn tất")
                .OrderByDescending(o => o.CreatedTime)
                .Select(o => new QuanAn.Models.ViewModel.OrderListVM
                {
                    OrderID = o.OrderID,
                    TableName = o.C_Table_.TableName,
                    Staff = o.C_User_.FullName,
                    CreatedTime = o.CreatedTime,
                    TotalFinal = o.Total,
                    Status = o.Status
                })
                .ToList();

            return View(orderListVM);
        }
    }
}