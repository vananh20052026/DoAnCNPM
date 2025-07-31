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

        // POST: OrderList/ChangeStatus
        [HttpPost]
        public ActionResult ChangeStatus(string orderId, string status)
        {
            var masterOrderToUpdate = db.C_Order_.FirstOrDefault(o => o.OrderID == orderId);

            if (masterOrderToUpdate != null)
            {
                masterOrderToUpdate.Status = status;
                db.SaveChanges();
                TempData["SuccessMessage"] = $"Đã cập nhật trạng thái Đơn hàng ID {orderId} thành {status}.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Không tìm thấy Đơn hàng với Mã Đơn hàng {orderId}.";
            }
            return RedirectToAction("OrderList", new { statusFilter = Request["statusFilter"], tableFilter = Request["tableFilter"], dayFilter = Request["dayFilter"] });
        }

        // POST: OrderList/RemoveOrderItem
        [HttpPost]
        public ActionResult RemoveOrderItem(string orderId, string foodId)
        {
            var orderDetailToRemove = db.C_Order_Detail_.FirstOrDefault(od => od.OrderID == orderId && od.FoodID == foodId);

            if (orderDetailToRemove != null)
            {
                if (orderDetailToRemove.Status == "Chưa làm")
                {
                    db.C_Order_Detail_.Remove(orderDetailToRemove);
                    db.SaveChanges();

                    var masterOrder = db.C_Order_.Include(o => o.C_Order_Detail_).FirstOrDefault(o => o.OrderID == orderId);

                    if (masterOrder != null)
                    {
                        if (!masterOrder.C_Order_Detail_.Any())
                        {
                            db.C_Order_.Remove(masterOrder);
                            db.SaveChanges();
                            TempData["SuccessMessage"] = "Món ăn đã được xóa và đơn hàng rỗng cũng đã được xóa thành công!";
                        }
                        else
                        {
                            masterOrder.Total = masterOrder.C_Order_Detail_.Sum(od => (decimal?)od.Quantity * (od.UnitPrice ?? 0)) - (masterOrder.Discount ?? 0);

                            if (masterOrder.C_Order_Detail_.Any(od => od.Status == "Đang xử lý"))
                            {
                                masterOrder.Status = "Đang xử lý";
                            }
                            else if (masterOrder.C_Order_Detail_.All(od => od.Status == "Hoàn tất"))
                            {
                                masterOrder.Status = "Hoàn tất";
                            }
                            else
                            {
                                masterOrder.Status = "Chưa làm";
                            }
                            db.SaveChanges();
                            TempData["SuccessMessage"] = "Món ăn đã được xóa khỏi đơn hàng và thông tin đơn hàng đã được cập nhật!";
                        }
                    }
                    else
                    {
                        TempData["SuccessMessage"] = "Món ăn đã được xóa. Không tìm thấy đơn hàng chính để cập nhật.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Không thể xóa món ăn vì trạng thái không phải **'Chưa làm'**.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không tìm thấy món ăn để xóa trong đơn hàng này.";
            }

            return RedirectToAction("OrderList", new { statusFilter = Request["statusFilter"], tableFilter = Request["tableFilter"], dayFilter = Request["dayFilter"] });
        }
    }
}