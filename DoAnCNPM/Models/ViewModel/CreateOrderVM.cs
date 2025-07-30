using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class CreateOrderVM
    {

        public string TableName { get; set; }
        public List<OrderItemVM> Items { get; set; }
    }
}