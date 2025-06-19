using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace QuanAn.Models.ViewModel
{
    public class MenuProductVM
    {
        //tiêu chí để search theo tên, mô tả sản phẩm
        //hoặc loại sản phẩm
        public string SearchTerm { get; set; }

        //các thuộc tính hỗ trợ phân trang 
        public int PageNumber { get; set; } //trang hiện tại
        public int PageSize { get; set; } = 10; //số sp mỗi trang


        //ds sp đã phân trang (giày, áo, ..)
        public List<C_Food_Info_> itemFoods { get; set; }
        public string user = HttpContext.Current.User.Identity.Name;
        public List<C_Category_> categories { get; set; }
    }
    public class OrderViewModel
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public List<OrderDetailViewModel> OrderDetails { get; set; }
    }

    public class OrderDetailViewModel
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice{get; set;}
    }
}