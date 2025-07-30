// Models/ViewModel/OrderBillManagementVM.cs
using System.Collections.Generic;

namespace QuanAn.Models.ViewModel
{
    public class OrderBillManagementVM
    {
        public IEnumerable<OrderListVM> Orders { get; set; } // Sử dụng OrderListVM để có thông tin chính của Order
        public IEnumerable<OrderItemVM> OrderDetails { get; set; } // Tất cả chi tiết các món ăn của các Order
    }
}