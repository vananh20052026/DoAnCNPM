using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class BillListVM
    {
        public string BillID { get; set; }
        public string CreatedTimeFormatted { get; set; } 
        public string StaffName { get; set; } 
        public string Payment { get; set; }
        public string TotalFormatted { get; set; }
        public string DiscountFormatted { get; set; }
        public string TotalFinalFormatted { get; set; }
        public string VATAmountFormatted { get; set; }
        public string TotalFinalWithVATFormatted { get; set; }
        public string OrderID { get; set; }
        public List<string> RelatedOrderIDs { get; set; }
        public string RelatedOrderIDsFormatted { get; set; }
        public List<AggregatedFoodItemVM> AggregatedFoodItems { get; set; }
    }
}