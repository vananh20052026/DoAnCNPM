using PagedList;
using QuanAn.Models;
using QuanAn.Models.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace QuanAn.Controllers
{
    public class MenuController : Controller
    {
       private QLQuanAnEntities db = new QLQuanAnEntities();
       
        public ActionResult Menu(string searchTerm, int? page)
        {
            var model = new MenuProductVM();
            var foods = db.C_Food_Info_.AsQueryable();
            var category = db.C_Category_.AsQueryable();
            //Tìm kiếm dựa trên từ khóa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                foods = foods.Where(f => f.FoodName.Contains(searchTerm)
                                    || f.Description.Contains(searchTerm)
                                    || f.C_Category_.CateName.Contains(searchTerm));
            }

            //Đoạn code liên quan tới phân trang
            //Lấy số trang hiện tại(mặc định là trang 1 nếu không có giá trị)
            //int pageNumber = page ?? 1;
            //int pageSize = 6; //Số sản phẩm mỗi trang

            //lấy top 10 sp bán chạy nhất
            //model.GiayProducts = products.OrderByDescending(p => p.OrderDetails.Count()).Take(10).ToList();
            //count() đang tính theo số lượt mua
            //tính theo tổng số lượng mua thì sửa thành Sum().Quantity

            //lấy 20 sp bán chậm nhất và phân trang 
            model.itemFoods = foods.ToList();
            model.categories = category.ToList();

            return View(model);
        }

        //GET: HOME/FoodDetail/5
        public ActionResult FoodDetail(int? id, int? quantity, int? page)
        {
            //id mã sản phẩm, quantity số lượng đặt mua,
            //page vị trí phần phân trang của các sản phẩm bán chạy
            if (id == null)
            {
                return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            }
            C_Food_Info_ food = db.C_Food_Info_.Find(id);

            if (food == null)
            {
                return HttpNotFound();
            }

            //lấy tất cả sản phẩm cùng danh mục 
            var foods = db.C_Food_Info_.Where(f => f.CateID == f.CateID && f.FoodID != f.FoodID).AsQueryable();

            FoodInfoVM model = new FoodInfoVM();

            //Đoạn code liên quan tới phân trang
            //lấy số trang hiện tại(mặc định là 1 nếu không có giá trị)
            int pageNumber = page ?? 1;
            int pageSize = model.PageSize; //số sản phẩm mỗi trang
            model.food_Info = food;
            model.RelatedProducts = foods.OrderBy(f => f.FoodID).Take(8).ToPagedList(pageNumber, pageSize);
            model.TopProducts = foods.OrderByDescending(f => f.C_Order_Detail_.Count()).Take(8).ToPagedList(pageNumber, pageSize);

            if (quantity.HasValue)
            {
                model.quantity = quantity.Value;
            }

            return View(model);
        }

        public ActionResult FoodList(string searchTerm, int? page)
        {
            var model = new MenuProductVM();
            var foods = db.C_Food_Info_.AsQueryable();

            //Tìm kiếm dựa trên từ khóa
            if (!string.IsNullOrEmpty(searchTerm))
            {
                model.SearchTerm = searchTerm;
                foods = foods.Where(f => f.FoodName.Contains(searchTerm)
                                    || f.Description.Contains(searchTerm)
                                    || f.C_Category_.CateName.Contains(searchTerm));
            }

            //Đoạn code liên quan tới phân trang
            //Lấy số trang hiện tại(mặc định là trang 1 nếu không có giá trị)
            int pageNumber = page ?? 1;
            int pageSize = 6; //Số sản phẩm mỗi trang

            //lấy top 10 sp bán chạy nhất
            //model.GiayProducts = products.OrderByDescending(p => p.OrderDetails.Count()).Take(10).ToList();
            //count() đang tính theo số lượt mua
            //tính theo tổng số lượng mua thì sửa thành Sum().Quantity

            //lấy 20 sp bán chậm nhất và phân trang 
            model.itemFoods = foods.OrderBy(f => f.C_Order_Detail_.Count()).ToList();
            //model.GiayProducts = products.OrderBy(p => p.OrderDetails.Count()).Take(20).ToPagedList(pageNumber, pageSize);

            //model.AoProducts = products.OrderBy(p => p.OrderDetails.Count()).Take(20).ToPagedList(pageNumber, pageSize);

            //model.PhukienProducts = products.OrderBy(p => p.OrderDetails.Count()).Take(20).ToPagedList(pageNumber, pageSize);

            return View(model);
        }

    }
}