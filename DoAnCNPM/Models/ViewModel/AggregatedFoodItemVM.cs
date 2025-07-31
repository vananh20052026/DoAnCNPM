using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class AggregatedFoodItemVM
    {
        public string FoodName { get; set; }
        public int TotalQuantity { get; set; } // Tổng số lượng của món ăn này
        public decimal UnitPrice { get; set; } // Giữ đơn giá (giả định đơn giá không đổi khi gộp)
        public decimal TotalPrice { get; set; } // Tổng tiền cho món ăn này (TotalQuantity * UnitPrice)

        public string TotalQuantityFormatted => TotalQuantity.ToString("N0");
        public string UnitPriceFormatted => UnitPrice.ToString("N0") + " đ";
        public string TotalPriceFormatted => TotalPrice.ToString("N0") + " đ";
    }
}