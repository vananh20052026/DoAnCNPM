//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace DoAnCNPM.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class C_Bill_Detail_
    {
        public string BillID { get; set; }
        public string OrderID { get; set; }
        public Nullable<int> Quantity { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
    
        public virtual C_Bill_ C_Bill_ { get; set; }
        public virtual C_Order_ C_Order_ { get; set; }
    }
}
