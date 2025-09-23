using CarRental.Models;
using CarRentalMS.Web.Data;
using CarRentalMS.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRentalMS.Web.Controllers
{
    [Authorize]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public StaffController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // ================= STAFF ROLE =================

        //[Authorize(Roles = "Staff")]
        //public IActionResult StaffDashboard()
        //{
        //    return View();
        //}

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffProfile()
        {
            var userName = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userName)) return Unauthorized();

            var user = await _context.User.FirstOrDefaultAsync(u => u.UserName == userName);
            if (user == null) return Unauthorized();

            var staff = await _context.Staffs.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (staff == null) return NotFound();

            return View("StaffProfile", staff);
        }

        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();
            return View("EditStaff", staff);
        }

        [Authorize(Roles = "Admin,Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Staff updatedStaff, IFormFile? profilePicture)
        {
            if (updatedStaff == null) return BadRequest();

            var staff = await _context.Staffs.FindAsync(updatedStaff.Id);
            if (staff == null) return NotFound();

            staff.Name = updatedStaff.Name;
            staff.Address = updatedStaff.Address;
            staff.EmailId = updatedStaff.EmailId;
            staff.PhoneNumber = updatedStaff.PhoneNumber;
            staff.IsActive = updatedStaff.IsActive;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(profilePicture.FileName);
                var uploadPath = Path.Combine("wwwroot/images", fileName);

                using var stream = new FileStream(uploadPath, FileMode.Create);
                await profilePicture.CopyToAsync(stream);

                staff.ProfilePictureUrl = "/images/" + fileName;
            }

            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync();

            // ✅ Save a success message in TempData
            TempData["SuccessMessage"] = "Profile updated successfully!";

            // ✅ Stay on StaffProfile instead of redirecting to StaffList
            return RedirectToAction("StaffProfile");
        }



        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Customers()
        {
            var customers = await _context.Customer
                .Include(c => c.User)      // Include User entity
                .AsNoTracking()
                .OrderBy(c => c.Name ?? c.User!.UserName)
                .ToListAsync();

            return View("Customers", customers);
        }

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> CustomerDetails(Guid id)
        {
            var customer = await _context.Customer
                .Include(c => c.User)      // Include User for details
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (customer == null) return NotFound();
            return View("CustomerDetails", customer);
        }


        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> Notifications()
        {
            var feedback = await _context.Feedback
                .AsNoTracking()
                .OrderByDescending(f => f.Id)
                .ToListAsync();
            return View("Notifications", feedback);
        }

        [Authorize(Roles = "Staff")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assist(string toEmail, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(toEmail) || string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(message))
            {
                TempData["AssistError"] = "Please provide email, subject and message.";
                return RedirectToAction(nameof(Notifications));
            }

            await _emailService.SendEmailAsync(toEmail, subject, message);
            TempData["AssistSuccess"] = "Assistance email sent successfully.";
            return RedirectToAction(nameof(Notifications));
        }

        [Authorize(Roles = "Staff")]
        public async Task<IActionResult> StaffDashboard()
        {
            ViewBag.TotalCars = await _context.Cars.CountAsync();
            ViewBag.TotalCustomers = await _context.Customer.CountAsync();
            ViewBag.TotalFeedback = await _context.Feedback.CountAsync();

            return View();
        }


        // ================= ADMIN ROLE =================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> StaffList()
        {
            var staffList = await _context.Staffs.ToListAsync();
            return View("StaffList", staffList);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult StaffManagement()
        {
            return View("StaffManagement");
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateStaff()
        {
            return View("CreateStaff");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(string email, string password, string name, string address, string phoneNumber)
        {
            var existingUser = await _context.User.FirstOrDefaultAsync(u => u.UserName == email);
            if (existingUser != null)
            {
                ViewBag.Error = "Email already exists.";
                return View("CreateStaff");
            }

            using var transaction = _context.Database.BeginTransaction();
            try
            {
                var newUser = new User
                {
                    UserName = email,
                    Password = password,
                    Role = "Staff"
                };
                _context.User.Add(newUser);
                await _context.SaveChangesAsync();

                var newStaff = new Staff
                {
                    Id = Guid.NewGuid(),
                    UserId = newUser.Id,
                    Name = name,
                    Address = address,
                    EmailId = email,
                    PhoneNumber = phoneNumber,
                    IsActive = true,
                    DateJoined = DateTime.Now
                };
                _context.Staffs.Add(newStaff);
                await _context.SaveChangesAsync();

                transaction.Commit();
                TempData["SuccessMessage"] = "Staff added successfully!";
                return RedirectToAction("StaffList");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                ViewBag.Error = "Error creating staff: " + (ex.InnerException?.Message ?? ex.Message);
                return View("CreateStaff");
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(Guid id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();
            return View("DetailsStaff", staff);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminEdit(Guid id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();
            return View("EditStaff", staff);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdminEdit(Staff updatedStaff)
        {
            if (!ModelState.IsValid) return View("EditStaff", updatedStaff);

            var staff = await _context.Staffs.FindAsync(updatedStaff.Id);
            if (staff == null) return NotFound();

            staff.Name = updatedStaff.Name;
            staff.Address = updatedStaff.Address;
            staff.EmailId = updatedStaff.EmailId;
            staff.PhoneNumber = updatedStaff.PhoneNumber;
            staff.IsActive = updatedStaff.IsActive;

            _context.Staffs.Update(staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Staff updated successfully!";
            return RedirectToAction("StaffList");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var staff = await _context.Staffs.FindAsync(id);
            if (staff == null) return NotFound();

            _context.Staffs.Remove(staff);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Staff deleted successfully!";
            return RedirectToAction("StaffList");
        }
    }
}
