using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class ReportVM
    {
        public List<OrderListVM> Orders { get; set; }
        public List<OrderItemVM> Items { get; set; }
        public List<MonthlyRevenueVM> MonthlyRevenues { get; set; }
        public List<TopFoodVM> TopOrderedFoods { get; set; }
        public List<TopFoodVM> TopProfitFoods { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrderCount { get; set; }      // Tổng số lượng order
        public int TotalFoodCount { get; set; }       // Tổng số món đã bán
    }

    public class MonthlyRevenueVM
    {
        public string Month { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class TopFoodVM
    {
        public string FoodName { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalProfit { get; set; }
    }
}