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
    
    public partial class C_Table_
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public C_Table_()
        {
            this.C_Order_ = new HashSet<C_Order_>();
        }
    
        public string TableID { get; set; }
        public string TableName { get; set; }
        public Nullable<int> NumOfSeats { get; set; }
        public string Status { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<C_Order_> C_Order_ { get; set; }
    }
}
