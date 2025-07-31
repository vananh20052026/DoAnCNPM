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

        // POST: OrderList/AddFoodToOrder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddFoodToOrder(string orderId, string foodId, int quantity)
        {
            if (string.IsNullOrEmpty(orderId) || string.IsNullOrEmpty(foodId) || quantity <= 0)
            {
                TempData["ErrorMessage"] = "Thông tin món ăn hoặc số lượng không hợp lệ.";
                return RedirectToAction("AddFoodToOrder", new { orderId = orderId });
            }

            var masterOrder = db.C_Order_.Include(o => o.C_Order_Detail_).FirstOrDefault(o => o.OrderID == orderId);
            var foodToAdd = db.C_Food_Info_.FirstOrDefault(f => f.FoodID == foodId);

            if (masterOrder == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng với mã **{orderId}**.";
                return RedirectToAction("OrderList");
            }

            if (foodToAdd == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy món ăn đã chọn.";
                return RedirectToAction("AddFoodToOrder", new { orderId = orderId });
            }

            var existingOrderDetail = masterOrder.C_Order_Detail_.FirstOrDefault(od => od.FoodID == foodId);

            if (existingOrderDetail != null)
            {
                // Món ăn đã có trong đơn hàng, cập nhật số lượng
                existingOrderDetail.Quantity += quantity;
                existingOrderDetail.Status = "Chưa làm"; // Đặt lại trạng thái món thành 'Chưa làm' nếu thêm số lượng
            }
            else
            {
                // Món ăn chưa có trong đơn hàng, thêm mới
                var newOrderDetail = new C_Order_Detail_
                {
                    OrderID = orderId,
                    FoodID = foodId,
                    Quantity = quantity,
                    UnitPrice = foodToAdd.UnitPrice,
                    Status = "Chưa làm" // Món mới thêm mặc định là 'Chưa làm'
                };
                db.C_Order_Detail_.Add(newOrderDetail);
            }

            // Cập nhật tổng tiền của đơn hàng
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

            // Nếu đơn hàng trước đó là 'Hoàn tất' và giờ thêm món mới, chuyển về 'Đang xử lý' hoặc 'Chưa làm'
            // Tránh trường hợp đơn hàng đã hoàn tất nhưng lại thêm món mới mà trạng thái vẫn là hoàn tất.
            if (masterOrder.Status == "Hoàn tất" && masterOrder.C_Order_Detail_.Any(od => od.Status == "Chưa làm"))
            {
                masterOrder.Status = "Chưa làm";
            }


            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã thêm {quantity} món {foodToAdd.FoodName} vào đơn hàng {orderId} thành công.";
            return RedirectToAction("OrderList", new { statusFilter = Request["statusFilter"], tableFilter = Request["tableFilter"], dayFilter = Request["dayFilter"] });
        }

        // GET: OrderList/CreateBill
        [HttpGet]
        public ActionResult CreateBill(string orderId)
        {
            if (string.IsNullOrEmpty(orderId))
            {
                TempData["ErrorMessage"] = "Không có mã đơn hàng được cung cấp để tạo hóa đơn.";
                return RedirectToAction("OrderList");
            }

            var order = db.C_Order_
                                .Include(o => o.C_Order_Detail_)
                                .Include(o => o.C_Table_)
                                .FirstOrDefault(o => o.OrderID == orderId);

            if (order == null)
            {
                TempData["ErrorMessage"] = $"Không tìm thấy đơn hàng với mã {orderId}.";
                return RedirectToAction("OrderList");
            }

            // Only allow billing for orders with 'Hoàn tất' status
            if (order.Status != "Hoàn tất")
            {
                TempData["ErrorMessage"] = $"Chỉ có thể tạo hóa đơn cho đơn hàng có trạng thái **'Hoàn tất'**. Đơn hàng **{orderId}** hiện đang là **'{order.Status}'**.";
                return RedirectToAction("OrderList");
            }

            // Check if bill already exists for this order
            if (db.C_Bill_.Any(b => b.OrderID == orderId))
            {
                TempData["ErrorMessage"] = $"Hóa đơn đã tồn tại cho đơn hàng **{orderId}**.";
                return RedirectToAction("OrderList");
            }

            string newBillID = orderId;

            if (db.C_Bill_.Any(b => b.BillID == newBillID))
            {
                TempData["ErrorMessage"] = $"Mã hóa đơn **{newBillID}** đã tồn tại. Không thể tạo hóa đơn trùng lặp cho đơn hàng này.";
                return RedirectToAction("OrderList");
            }

            var newBill = new C_Bill_
            {
                BillID = newBillID,
                Total = order.Total,
                Discount = order.Discount,
                TotalFinal = (order.Total ?? 0) - (order.Discount ?? 0),
                Payment = "Chưa thanh toán",
                CreatedTime = DateTime.Now,
                OrderID = order.OrderID,
                UserID = Session["UserID"] as string
            };
            db.C_Bill_.Add(newBill);

            int totalQuantityInOrder = 0;
            decimal totalAmountInOrder = 0;

            foreach (var detail in order.C_Order_Detail_)
            {
                totalQuantityInOrder += (int)detail.Quantity;
                totalAmountInOrder += (detail.Quantity ?? 0) * (detail.UnitPrice ?? 0);
            }

            var billDetail = new C_Bill_Detail_
            {
                BillID = newBillID,
                OrderID = order.OrderID,
                Quantity = totalQuantityInOrder,
                UnitPrice = totalAmountInOrder
            };
            db.C_Bill_Detail_.Add(billDetail);

            // Update order status to "Đã tạo bill"
            order.Status = "Đã tạo bill";

            // Optionally, set table status to available if needed
            if (order.C_Table_ != null)
            {
                order.C_Table_.Status = "Trống";
            }

            db.SaveChanges();

            TempData["SuccessMessage"] = $"Đã tạo hóa đơn {newBillID} thành công cho đơn hàng {orderId}. Trạng thái đơn hàng đã được cập nhật thành 'Đã tạo bill'.";
            return RedirectToAction("OrderList");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessMergedBill(List<string> selectedOrderIds)
        {
            if (selectedOrderIds == null || !selectedOrderIds.Any())
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất một đơn hàng để gộp.";
                return RedirectToAction("OrderList");
            }

            try
            {
                var ordersToMerge = db.C_Order_
                                      .Include(o => o.C_Order_Detail_)
                                      .Include(o => o.C_Table_)
                                      .Where(o => selectedOrderIds.Contains(o.OrderID))
                                      .ToList();

                if (ordersToMerge.Count != selectedOrderIds.Count)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy tất cả các đơn hàng đã chọn.";
                    return RedirectToAction("OrderList");
                }

                if (ordersToMerge.Any(o => o.Status != "Hoàn tất"))
                {
                    TempData["ErrorMessage"] = "Chỉ có thể gộp các đơn hàng đã 'Hoàn tất'.";
                    return RedirectToAction("OrderList");
                }

                string mergedBillId = selectedOrderIds.First();
                if (db.C_Bill_.Any(b => b.BillID == mergedBillId))
                {
                    TempData["ErrorMessage"] = $"Hóa đơn {mergedBillId} đã tồn tại.";
                    return RedirectToAction("OrderList");
                }

                decimal? totalSum = 0;
                decimal? discountSum = 0;
                string staffId = Session["UserID"] as string;
                DateTime createdTime = DateTime.Now;

                foreach (var order in ordersToMerge)
                {
                    totalSum += order.Total;
                    discountSum += order.Discount;

                    order.Status = "Đã tạo bill";
                    if (order.C_Table_ != null)
                        order.C_Table_.Status = "Trống";

                    foreach (var detail in order.C_Order_Detail_)
                        detail.Status = "Hoàn tất";
                }

                var newBill = new C_Bill_
                {
                    BillID = mergedBillId,
                    Total = totalSum,
                    Discount = discountSum,
                    TotalFinal = (totalSum ?? 0) - (discountSum ?? 0),
                    Payment = "Chưa thanh toán",
                    CreatedTime = createdTime,
                    UserID = staffId
                };
                db.C_Bill_.Add(newBill);

                foreach (var order in ordersToMerge)
                {
                    var totalQuantity = order.C_Order_Detail_.Sum(d => d.Quantity ?? 0);
                    var totalAmount = order.C_Order_Detail_.Sum(d => (d.Quantity ?? 0) * (d.UnitPrice ?? 0));

                    var billDetail = new C_Bill_Detail_
                    {
                        BillID = mergedBillId,
                        OrderID = order.OrderID,
                        Quantity = totalQuantity,
                        UnitPrice = totalAmount
                    };
                    db.C_Bill_Detail_.Add(billDetail);
                }

                db.SaveChanges();
                TempData["SuccessMessage"] = $"Đã gộp thành công các đơn hàng và tạo hóa đơn {mergedBillId}.";
                return RedirectToAction("OrderList");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Lỗi hệ thống khi gộp đơn hàng: " + ex.Message;
                return RedirectToAction("OrderList");
            }
        }
    }
}