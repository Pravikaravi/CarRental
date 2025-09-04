using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Mvc;

namespace CarRentalMS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.User.FirstOrDefault(u => u.UserName == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("Role", user.Role);
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // GET: /Account/Signup
        public IActionResult Signup()
        {
            return View();
        }

        // POST: /Account/Signup
        [HttpPost]
        public IActionResult Signup(string email, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ViewBag.Error = "Passwords do not match.";
                return View();
            }

            // Check if email already exists
            var existingUser = _context.User.FirstOrDefault(u => u.UserName == email);
            if (existingUser != null)
            {
                ViewBag.Error = "Email already exists.";
                return View();
            }

            var newUser = new User
            {
                UserName = email,
                Password = password,
                Role = "Customer" // default role
            };

            _context.User.Add(newUser);
            _context.SaveChanges();

            // Instead of logging in immediately, show a message
            TempData["SuccessMessage"] = "Account created successfully! Please login to continue.";

            return RedirectToAction("Login"); // Redirect to Login page
        }


        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
