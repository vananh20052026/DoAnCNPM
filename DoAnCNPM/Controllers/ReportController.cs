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

            // TODO: Implement report logic
            var reportVM = new ReportVM();
            
            return View(reportVM);
        }
    }
}
