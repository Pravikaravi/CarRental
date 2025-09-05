using Microsoft.AspNetCore.Mvc;

namespace CarRental.Controllers
{
    public class CustomerController : Controller
    {
        public IActionResult CustomerDashboard()
        {
            return View();
        }
    }
}
