using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class OrderListVM
    {
        public string OrderID { get; set; }
        public DateTime? CreatedTime { get; set; }
        public string TableName { get; set; }
        public string Staff { get; set; }
        public decimal? TotalFinal { get; set; }
        public decimal? Discount { get; set; }
        public string Status { get; set; }
        public object UnitPrice { get; internal set; }
        public List<OrderItemVM> OrderItems { get; set; }
    }
}