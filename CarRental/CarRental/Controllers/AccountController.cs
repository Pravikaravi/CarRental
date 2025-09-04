using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Mvc;
using MailKit.Net.Smtp;
using MimeKit;

namespace CarRentalMS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Signup
        public IActionResult Signup()
        {
            return View();
        }

        // POST: /Account/Signup
        [HttpPost]
        public IActionResult Signup(string email, string password, string confirmPassword, string otpInput)
        {
            // ✅ Step 1: If OTP is not yet entered → send OTP
            if (string.IsNullOrEmpty(otpInput))
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

                // ✅ Generate OTP
                var otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("OTP", otp);
                HttpContext.Session.SetString("TempEmail", email);
                HttpContext.Session.SetString("TempPassword", password);

                // ✅ Send OTP email
                SendOtpEmail(email, otp);

                // ✅ Show OTP form in same page
                ViewBag.ShowOtpForm = true;
                return View();
            }

            // ✅ Step 2: If OTP is provided → verify OTP
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var tempEmail = HttpContext.Session.GetString("TempEmail");
            var tempPassword = HttpContext.Session.GetString("TempPassword");

            if (otpInput == sessionOtp)
            {
                var newUser = new User
                {
                    UserName = tempEmail,
                    Password = tempPassword,
                    Role = "Customer"
                };

                _context.User.Add(newUser);
                _context.SaveChanges();

                // Clear session
                HttpContext.Session.Remove("OTP");
                HttpContext.Session.Remove("TempEmail");
                HttpContext.Session.Remove("TempPassword");

                TempData["SuccessMessage"] = "Account created successfully! Please login.";
                return RedirectToAction("Login");
            }

            ViewBag.Error = "Invalid OTP. Please try again.";
            ViewBag.ShowOtpForm = true;
            return View();
        }

        // ✅ Helper: Send OTP Email
        private void SendOtpEmail(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CarRental System", "cargorental2@gmail.com"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "CarRental Signup OTP";
            message.Body = new TextPart("plain")
            {
                Text = $"Your OTP is: {otp}"
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate("cargorental2@gmail.com", "eygr tqyr oxjw ewyw"); // <-- App Password here
                client.Send(message);
                client.Disconnect(true);
            }
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

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
