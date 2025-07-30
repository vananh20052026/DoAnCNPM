using QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Data.Entity.Validation;
using System.Linq;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace QuanAn.Controllers
{
    [Authorize]
    public class TableController : Controller
    {
        private QLQuanAnEntities db = new QLQuanAnEntities();

        public ActionResult TableList()
        {
            var tl = new TableVM
            {
                TableList = db.C_Table_.ToList()
            };
            return View(tl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SelectTable(string tableId)
        {
            if (string.IsNullOrEmpty(tableId))
            {
                //TempData["ErrorMessage"] = "Vui lòng chọn một bàn để tạo đơn hàng.";
                return RedirectToAction("TableList", "Table");
            }

            var selectedTable = db.C_Table_.FirstOrDefault(t => t.TableID.Trim() == tableId.Trim());

            if (selectedTable == null)
            {
                //TempData["ErrorMessage"] = "Bàn bạn chọn không hợp lệ hoặc không tồn tại.";
                return RedirectToAction("TableList", "Table");
            }
            return RedirectToAction("Menu", "Menu", new { tableName = selectedTable.TableName.Trim(), orderId = (string)null });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditTable(string TableID, string TableName, int NumOfSeats)
        {
            var table = db.C_Table_.FirstOrDefault(t => t.TableID.Trim() == TableID.Trim()); // ✅ đúng

            if (table != null)
            {
                table.TableName = TableName;
                table.NumOfSeats = NumOfSeats;
                db.SaveChanges();

                TempData["Message"] = "✅ Cập nhật thông tin bàn thành công!";
            }
            else
            {
                TempData["Message"] = "❌ Không tìm thấy bàn cần sửa.";
            }

            return RedirectToAction("TableList", "Table");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddTable(string TableName, int NumOfSeats)
        {
            try
            {
                // Kiểm tra tên bàn rỗng hoặc số ghế không hợp lệ
                if (string.IsNullOrWhiteSpace(TableName) || NumOfSeats <= 0)
                {
                    TempData["AddTableError"] = "true";
                    return RedirectToAction("TableList", "Table");
                }

                // Kiểm tra trùng tên bàn (không phân biệt hoa thường, bỏ khoảng trắng)
                var existingTable = db.C_Table_
                    .FirstOrDefault(t => t.TableName.Trim().ToLower() == TableName.Trim().ToLower());
                if (existingTable != null)
                {
                    TempData["AddTableError"] = $"❌ Tên bàn '{TableName}' đã tồn tại!";
                    return RedirectToAction("TableList", "Table");
                }

                // Tìm ID lớn nhất đang có, rồi +1
                int newId = 1;

                var maxIdStr = db.C_Table_
                    .Select(t => t.TableID.Trim())
                    .ToList()
                    .Where(id => int.TryParse(id, out _))
                    .Select(id => int.Parse(id))
                    .DefaultIfEmpty(0)
                    .Max();

                newId = maxIdStr + 1;

                string generatedId = newId.ToString(); // ví dụ: "9"

                // Tạo đối tượng bàn mới
                var newTable = new C_Table_
                {
                    TableID = generatedId,
                    TableName = TableName.Trim(),
                    NumOfSeats = NumOfSeats,
                    Status = "Available"
                };

                db.C_Table_.Add(newTable);
                db.SaveChanges();

                TempData["Message"] = "✅ Thêm bàn mới thành công!";
            }
            catch (DbEntityValidationException ex)
            {
                var errors = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(e => $"Property: {e.PropertyName}, Error: {e.ErrorMessage}");

                System.Diagnostics.Debug.WriteLine("Validation Errors: " + string.Join("; ", errors));

                TempData["AddTableError"] = "true";
            }

            return RedirectToAction("TableList", "Table");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteTable(string TableID)
        {
            if (string.IsNullOrWhiteSpace(TableID))
                return RedirectToAction("TableList", "Table");

            var table = db.C_Table_.Find(TableID);
            if (table != null)
            {
                if ((table.Status ?? "").Trim().ToLower() == "occupied")
                {
                    TempData["ErrorMessage"] = $"❌ Không thể xoá bàn '{table.TableName}' vì đang được sử dụng!";
                }
                else
                {
                    db.C_Table_.Remove(table);
                    db.SaveChanges();
                    TempData["Message"] = $"✅ Đã xoá bàn '{table.TableName}' thành công!";
                }
            }

            return RedirectToAction("TableList", "Table");
        }
    }
}