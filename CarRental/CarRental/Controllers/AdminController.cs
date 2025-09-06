using Microsoft.AspNetCore.Mvc;


namespace YourNamespace.Controllers
{
    public class AdminController : Controller
    {
        // GET: Admin/AdminDashboard
        public ActionResult AdminDashboard()
        {
            ViewBag.Title = "AdminDashboard";
            return View();
        }

        // GET: Admin/AdminProfile
        public ActionResult AdminProfile()
        {
            ViewBag.Title = "AdminProfile";
            return View();
        }

        // GET: Admin/AdminNotifications
        public ActionResult AdminNotifications()
        {
            ViewBag.Title = "AdminNotifications";
            return View();
        }

        // GET: Admin/StaffManagement
        public ActionResult StaffManagement()
        {
            ViewBag.Title = "StaffManagement";
            return View();
        }

        // GET: Admin/BookingManagement
        public ActionResult BookingManagement()
        {
            ViewBag.Title = "BookingManagement";
            return View();
        }

        // GET: Admin/CarManagement
        public ActionResult CarManagement()
        {
            ViewBag.Title = "CarManagement";
            return View();
        }

        // GET: Admin/NewBooking
        public ActionResult NewBooking()
        {
            ViewBag.Title = "NewBooking";
            return View();
        }
    }
}
