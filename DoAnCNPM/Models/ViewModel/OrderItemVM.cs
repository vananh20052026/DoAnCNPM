using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class OrderItemVM
    {
        public string OrderID { get; set; }
        public string FoodID { get; set; }
        public string FoodName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Status { get; set; }
    }
}