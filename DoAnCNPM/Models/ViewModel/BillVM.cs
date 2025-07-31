using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class BillVM
    {
        public List<BillListVM> Bills { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalBills { get; set; }
        public int TotalPages { get; set; }
    }
}