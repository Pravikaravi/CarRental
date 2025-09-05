using Microsoft.AspNetCore.Mvc;

namespace CarRental.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult AdminDashboard()
        {
            return View();
        }
    }
}
