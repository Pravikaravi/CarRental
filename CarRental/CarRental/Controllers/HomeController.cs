using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace CarRental.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // <-- add this

        // Inject logger AND DbContext
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // <-- assign it
        }

        //public IActionResult Index()
        //{
        //    return View();
        //}

        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        public IActionResult Aboutus()
        {
            return View();
        }




        //[HttpGet]
        //public IActionResult Contactus()
        //{
        //    var username = HttpContext.Session.GetString("UserName");
        //    if (string.IsNullOrEmpty(username))
        //    {
        //        TempData["ErrorMessage"] = "Please login to submit feedback.";
        //        return RedirectToAction("Login", "Account");
        //    }

        //    var model = new Feedback
        //    {
        //        Name = username // pre-fill logged-in username
        //    };

        //    return View(model);
        //}

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public IActionResult Contactus(Feedback model)
        //{
        //    var username = HttpContext.Session.GetString("UserName");
        //    if (string.IsNullOrEmpty(username))
        //    {
        //        TempData["ErrorMessage"] = "Please login to submit feedback.";
        //        return RedirectToAction("Login", "Account");
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        model.Id = Guid.NewGuid();
        //        model.Name = username; // ensure username is correct

        //        // Assign UserId if needed
        //        var user = _context.User.FirstOrDefault(u => u.UserName == username);
        //        if (user != null)
        //        {
        //            model.UserId = user.Id;
        //        }

        //        _context.Feedback.Add(model);
        //        _context.SaveChanges();

        //        ViewBag.Message = "Your feedback has been submitted successfully!";
        //        ModelState.Clear(); // clears the form
        //    }

        //    return View(new Feedback { Name = username }); // refill username after submit
        //}

        [HttpGet]
        public IActionResult Contactus()
        {
            var username = HttpContext.Session.GetString("UserName");

            var model = new Feedback();

            // Pre-fill username if logged in
            if (!string.IsNullOrEmpty(username))
            {
                model.Name = username;
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contactus(Feedback model)
        {
            var username = HttpContext.Session.GetString("UserName");

            // Redirect guest users to Login
            if (string.IsNullOrEmpty(username))
            {
                TempData["ErrorMessage"] = "Login to your account first!";
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Contactus", "Home") });

            }

            if (ModelState.IsValid)
            {
                model.Id = Guid.NewGuid();
                model.Name = username;

                var user = _context.User.FirstOrDefault(u => u.UserName == username);
                if (user != null)
                {
                    model.UserId = user.Id;
                }

                _context.Feedback.Add(model);
                _context.SaveChanges();

                ViewBag.Message = "Your feedback has been submitted successfully!";
                ModelState.Clear();
            }

            return View(new Feedback { Name = username });
        }





        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            return View();
        }

        public IActionResult Developers()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        // On Login GET
        //public IActionResult Login(string returnUrl = null)
        //{
        //    if (TempData["ReturnUrl"] != null)
        //        returnUrl = TempData["ReturnUrl"].ToString();

        //    ViewBag.ReturnUrl = returnUrl;
        //    return View();
        //}

        //// On Login POST
        //[HttpPost]
        //public IActionResult Login(string email, string password, string returnUrl = null)
        //{
        //    var user = _context.User.FirstOrDefault(u => u.UserName == email && u.Password == password);
        //    if (user != null)
        //    {
        //        HttpContext.Session.SetString("UserName", user.UserName);
        //        HttpContext.Session.SetString("Role", user.Role);
        //        HttpContext.Session.SetString("ProfilePic", user.ProfilePhotoUrl ?? "");

        //        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //            return Redirect(returnUrl);

        //        return RedirectToAction("Index", "Home");
        //    }

        //    ViewBag.Error = "Invalid email or password.";
        //    ViewBag.ReturnUrl = returnUrl;
        //    return View();
        //}




    }
}
