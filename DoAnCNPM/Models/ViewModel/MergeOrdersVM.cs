using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class MergeOrdersVM
    {
        public decimal? Discount { get; set; }

        public List<OrderListVM> MergeableOrders { get; set; } = new List<OrderListVM>();
    }
}