using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class FoodDetailVM
    {
        public C_Food_Info_ Food { get; set; }
        public List<IngredientDetail> Ingredients { get; set; }
        public decimal UnitPrice { get; set; } // ✅ Đúng vị trí

        public class IngredientDetail
        {
            public string IngreName { get; set; }
            public double Quantity { get; set; }
            public string UnitMeasurement { get; set; }
        }
    }
}