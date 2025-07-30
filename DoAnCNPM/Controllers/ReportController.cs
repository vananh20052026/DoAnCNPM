using QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanAn.Controllers
{
    public class ReportController : Controller
    {
        QLQuanAnEntities db = new QLQuanAnEntities();

        // GET: Report
        public ActionResult Report(string month)
        {
            QLQuanAnEntities db = new QLQuanAnEntities();

            // Get all orders and items
            var allOrders = (from o in db.C_Order_
                             join u in db.C_User_ on o.UserID equals u.UserID
                             join t in db.C_Table_ on o.TableID equals t.TableID into tableJoin
                             from t in tableJoin.DefaultIfEmpty()
                             select new OrderListVM
                             {
                                 OrderID = o.OrderID,
                                 CreatedTime = o.CreatedTime,
                                 TableName = t != null ? t.TableName : "Chưa chọn bàn",
                                 Staff = u.FullName,
                                 TotalFinal = o.Total,
                                 Discount = o.Discount,
                                 Status = o.Status
                             }).ToList();

            var allItems = (from od in db.C_Order_Detail_
                            join f in db.C_Food_Info_ on od.FoodID equals f.FoodID
                            select new OrderItemVM
                            {
                                OrderID = od.OrderID,
                                FoodID = od.FoodID,
                                FoodName = f.FoodName,
                                Quantity = od.Quantity ?? 0,
                                UnitPrice = od.UnitPrice ?? 0,
                                Status = od.Status
                            }).ToList();

            // Filter orders by month if selected
            List<OrderListVM> orders = allOrders;
            if (!string.IsNullOrEmpty(month))
            {
                orders = allOrders
                    .Where(o => o.CreatedTime.HasValue && o.CreatedTime.Value.ToString("yyyy-MM") == month)
                    .ToList();
            }

            // Filter items to only those in filtered orders
            var orderIds = orders.Select(o => o.OrderID).ToHashSet();
            var items = allItems.Where(i => orderIds.Contains(i.OrderID)).ToList();

            // Monthly revenue (for chart, only show the selected month if filtered)
            var monthlyRevenues = allOrders
                .Where(o => o.CreatedTime.HasValue)
                .GroupBy(o => o.CreatedTime.Value.ToString("yyyy-MM"))
                .Select(g => new MonthlyRevenueVM
                {
                    Month = g.Key,
                    TotalRevenue = g.Sum(x => x.TotalFinal ?? 0)
                })
                .OrderBy(x => x.Month)
                .ToList();

            if (!string.IsNullOrEmpty(month))
            {
                monthlyRevenues = monthlyRevenues.Where(x => x.Month == month).ToList();
            }

            // Top 5 most ordered món (filtered)
            var topOrderedFoods = items
                .GroupBy(i => i.FoodName)
                .Select(g => new TopFoodVM
                {
                    FoodName = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalProfit = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .Take(5)
                .ToList();

            // Top 5 most profit món (filtered)
            var topProfitFoods = items
                .GroupBy(i => i.FoodName)
                .Select(g => new TopFoodVM
                {
                    FoodName = g.Key,
                    TotalQuantity = g.Sum(x => x.Quantity),
                    TotalProfit = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.TotalProfit)
                .Take(5)
                .ToList();

            // Month list for filter dropdown
            ViewBag.SelectedMonth = month;
            ViewBag.MonthList = allOrders
                .Where(o => o.CreatedTime.HasValue)
                .Select(o => o.CreatedTime.Value.ToString("yyyy-MM"))
                .Distinct()
                .OrderByDescending(m => m)
                .ToList();

            var reportVM = new ReportVM
            {
                Orders = orders,
                Items = items,
                MonthlyRevenues = monthlyRevenues,
                TopOrderedFoods = topOrderedFoods,
                TopProfitFoods = topProfitFoods
            };

            reportVM.TotalRevenue = orders.Sum(x => x.TotalFinal ?? 0);
            reportVM.TotalOrderCount = orders.Count;
            reportVM.TotalFoodCount = items.Sum(x => x.Quantity);

            return View(reportVM);
        }
    }
}