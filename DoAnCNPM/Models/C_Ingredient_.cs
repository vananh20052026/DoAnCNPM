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
    
    public partial class C_Ingredient_
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public C_Ingredient_()
        {
            this.C_Recipe_Detail_ = new HashSet<C_Recipe_Detail_>();
        }
    
        public string IngreID { get; set; }
        public string IngreName { get; set; }
        public Nullable<long> Stock { get; set; }
        public string UnitMeasurement { get; set; }
    
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public virtual ICollection<C_Recipe_Detail_> C_Recipe_Detail_ { get; set; }
    }
}
