using CarRental.Models;
using CarRentalMS.Web.Data;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using MimeKit;
using System.Security.Claims;

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
            // Step 1: If OTP is not yet entered → send OTP
            if (string.IsNullOrEmpty(otpInput))
            {
                if (password != confirmPassword)
                {
                    ViewBag.Error = "Passwords do not match.";
                    return View();
                }

                // ✅ Add password strength check here
                if (password.Length < 6 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
                {
                    ViewBag.Error = "Password must be at least 6 characters and include uppercase, lowercase, and a number.";
                    return View();
                }

                var existingUser = _context.User.FirstOrDefault(u => u.UserName == email);
                if (existingUser != null)
                {
                    ViewBag.Error = "Email already exists.";
                    return View();
                }

                // Generate OTP
                var otp = new Random().Next(100000, 999999).ToString();
                HttpContext.Session.SetString("OTP", otp);
                HttpContext.Session.SetString("TempEmail", email);
                HttpContext.Session.SetString("TempPassword", password);
                HttpContext.Session.SetString("OTPExpiry", DateTime.Now.AddMinutes(2).ToString());

                SendOtpEmail(email, otp);

                ViewBag.ShowOtpForm = true;
                return View();
            }

            // Step 2: OTP verification
            var sessionOtp = HttpContext.Session.GetString("OTP");
            var tempEmail = HttpContext.Session.GetString("TempEmail");
            var tempPassword = HttpContext.Session.GetString("TempPassword");
            var expiryString = HttpContext.Session.GetString("OTPExpiry");

            if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(expiryString))
            {
                ViewBag.Error = "OTP expired or not found. Please try again.";
                return View();
            }

            var expiryTime = DateTime.Parse(expiryString);

            if (DateTime.Now > expiryTime)
            {
                HttpContext.Session.Clear();
                ViewBag.Error = "OTP has expired. Please request a new one.";
                return View();
            }

            if (otpInput == sessionOtp)
            {
                var newUser = new User
                {
                    UserName = tempEmail,
                    Password = tempPassword,
                    Role = "Customer",
                    Provider = null,
                    ProviderKey = null,
                    ProfilePhotoUrl = null
                };

                _context.User.Add(newUser);
                _context.SaveChanges();

                // ✅ Add Customer record
                var customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    UserId = newUser.Id,
                    Email = newUser.UserName
                };
                _context.Customer.Add(customer);
                _context.SaveChanges();

                HttpContext.Session.Clear();

                TempData["SuccessMessage"] = "Account created successfully! Please login.";
                return RedirectToAction("Login");
            }


            ViewBag.Error = "Invalid OTP. Please try again.";
            ViewBag.ShowOtpForm = true;
            return View();
        }

        // Helper: Send OTP Email
        private void SendOtpEmail(string toEmail, string otp)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("CarRental System", "cargorental2@gmail.com"));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = "CarRental Signup OTP";
            message.Body = new TextPart("plain")
            {
                Text = $"Your OTP is: {otp}\n\nThis OTP will expire in 2 minutes."
            };

            using (var client = new SmtpClient())
            {
                client.Connect("smtp.gmail.com", 587, false);
                client.Authenticate("cargorental2@gmail.com", "eygr tqyr oxjw ewyw"); // App password
                client.Send(message);
                client.Disconnect(true);
            }
        }

        // GET: /Account/Login
        //public IActionResult Login(string returnUrl = null)
        //{
        //    ViewBag.ReturnUrl = returnUrl; // store return URL
        //    return View();
        //}
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = null)
        {
            var user = _context.User.FirstOrDefault(u => u.UserName == email && u.Password == password);

            if (user != null)
            {
                // Create claims
                var claims = new List<Claim>
{
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ← Add this
    new Claim(ClaimTypes.Name, user.UserName),
    new Claim(ClaimTypes.Role, user.Role),
    new Claim("ProfilePic", user.ProfilePhotoUrl ?? "")
};


                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

                // Sign in
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, claimsPrincipal);

                // Optional: store in session
                HttpContext.Session.SetString("UserName", user.UserName);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("ProfilePic", user.ProfilePhotoUrl ?? "");

                // Redirect to Home, dashboard will be accessed via avatar dropdown
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid email or password.";
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }



        public IActionResult Login(string returnUrl = null)
        {
            if (TempData["ReturnUrl"] != null)
                returnUrl = TempData["ReturnUrl"].ToString();

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }


        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Google Login
        public IActionResult GoogleLogin(string returnUrl = "/")
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // Google Response
        public async Task<IActionResult> GoogleResponse(string returnUrl = "/")
        {
            var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var claims = result.Principal.Identities.FirstOrDefault().Claims;

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var picture = claims.FirstOrDefault(c => c.Type == "picture")?.Value;
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Google login failed. Please try again.";
                return RedirectToAction("Login");
            }

            var user = _context.User.FirstOrDefault(u => u.UserName == email);

            if (user == null)
            {
                user = new User
                {
                    UserName = email,
                    Password = null,
                    Role = "Customer",
                    Provider = "Google",
                    ProviderKey = googleId,
                    ProfilePhotoUrl = picture
                };

                _context.User.Add(user);
                _context.SaveChanges();
            }
            else
            {
                if (!string.IsNullOrEmpty(picture) && user.ProfilePhotoUrl != picture)
                {
                    user.ProfilePhotoUrl = picture;
                    _context.SaveChanges();
                }
            }

            // ✅ Add Customer entry if role = Customer
            if (user.Role == "Customer")
            {
                var existingCustomer = _context.Customer.FirstOrDefault(c => c.UserId == user.Id);
                if (existingCustomer == null)
                {
                    var customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Email = user.UserName
                    };
                    _context.Customer.Add(customer);
                    _context.SaveChanges();
                }
            }

            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetString("Role", user.Role);
            HttpContext.Session.SetString("ProfilePic", user.ProfilePhotoUrl ?? "");

            return Redirect(returnUrl);
        }




    }
}
