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
    
    public partial class C_Order_Detail_
    {
        public string FoodID { get; set; }
        public string OrderID { get; set; }
        public Nullable<decimal> UnitPrice { get; set; }
        public string Status { get; set; }
        public Nullable<int> Quantity { get; set; }
    
        public virtual C_Food_Info_ C_Food_Info_ { get; set; }
        public virtual C_Order_ C_Order_ { get; set; }
    }
}
