using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class TableVM
    {
        public List<C_Table_> TableList { get; set; }
        public int TableID { get; set; }
        public string TableName { get; set; }
        public Nullable<int> NumOfSeats { get; set; }
        public string Status { get; set; } 
    }
}