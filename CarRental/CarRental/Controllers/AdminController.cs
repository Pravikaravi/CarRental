using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace CarRentalMS.Web.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admin can access these actions
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // Dashboard
        public IActionResult AdminDashboard()
        {
            ViewBag.UserName = User.Identity.Name;

            int totalCars = _context.Cars.Count();
            int totalCustomers = _context.Customer.Count(); // Replace with your actual DbSet name
            int totalStaffs = _context.Staffs.Count();

            ViewBag.TotalCars = totalCars;
            ViewBag.TotalCustomers = totalCustomers;
            ViewBag.TotalStaffs = totalStaffs;

            return View();
        }


        [HttpGet]
        public IActionResult AdminProfile()
        {
            var userEmail = User.Identity.Name;
            var admin = _context.User.FirstOrDefault(u => u.UserName == userEmail && u.Role == "Admin");

            if (admin == null)
                return NotFound();

            return View(admin);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminProfile(int id, IFormFile ProfilePhoto, string Password)
        {
            var admin = await _context.User.FindAsync(id);

            if (admin == null || admin.Role != "Admin")
                return NotFound();

            // Update password if provided
            if (!string.IsNullOrWhiteSpace(Password))
            {
                admin.Password = Password; // Ideally hash this before saving
            }

            // Handle profile photo upload
            if (ProfilePhoto != null && ProfilePhoto.Length > 0)
            {
                var fileName = Path.GetFileName(ProfilePhoto.FileName);
                var filePath = Path.Combine("wwwroot/uploads", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePhoto.CopyToAsync(stream);
                }

                admin.ProfilePhotoUrl = "/uploads/" + fileName;
            }

            _context.Update(admin);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("AdminProfile");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfileImage(int id, IFormFile ProfilePhoto)
        {
            var admin = await _context.User.FindAsync(id);

            if (admin == null || admin.Role != "Admin")
                return NotFound();

            if (ProfilePhoto != null && ProfilePhoto.Length > 0)
            {
                var fileName = Path.GetFileName(ProfilePhoto.FileName);
                var filePath = Path.Combine("wwwroot/uploads", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProfilePhoto.CopyToAsync(stream);
                }

                admin.ProfilePhotoUrl = "/uploads/" + fileName;
                _context.Update(admin);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("AdminProfile");
        }


        // GET: Admin/StaffList
        public IActionResult StaffList()
        {
            var staffList = _context.Staffs.ToList();
            return View(staffList);
        }

        // GET: Admin/CreateStaff
        public IActionResult CreateStaff()
        {
            return View();
        }

        // POST: Admin/CreateStaff
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateStaff(string email, string password, string name, string address, string phoneNumber)
        {
            // check duplicate user
            var existingUser = _context.User.FirstOrDefault(u => u.UserName == email);
            if (existingUser != null)
            {
                ViewBag.Error = "Email already exists.";
                return View();
            }

            // 1. Create User
            var newUser = new User
            {
                UserName = email,
                Password = password,
                Role = "Staff",
                Provider = null,
                ProviderKey = null,
                ProfilePhotoUrl = null
            };
            _context.User.Add(newUser);
            _context.SaveChanges();

            // 2. Create Staff linked to User
            var newStaff = new Staff
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                Name = name,
                Address = address,
                EmailId = email,
                PhoneNumber = phoneNumber,
                IsActive = true,
                DateJoined = DateTime.Now,
                ProfilePictureUrl = null
            };
            _context.Staffs.Add(newStaff);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Staff added successfully!";
            return RedirectToAction("StaffList");
        }

        //public IActionResult AdminDashboard()
        //{
        //    if (HttpContext.Session.GetString("Role") != "Admin")
        //        return RedirectToAction("AccessDenied", "Account", new { ReturnUrl = "/Admin/AdminDashboard" });

        //    ViewBag.TotalCars = _context.Cars.Count();
        //    ViewBag.TotalStaffs = _context.Staffs.Count();
        //    ViewBag.TotalCustomers = _context.User.Count(u => u.Role == "Customer");
        //    ViewBag.TotalBookings = _context.Bookings.Count();

        //    return View();
        //}

    }
}
