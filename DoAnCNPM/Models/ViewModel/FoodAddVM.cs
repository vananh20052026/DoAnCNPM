using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class FoodAddVM
    {
        public string FoodID { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}