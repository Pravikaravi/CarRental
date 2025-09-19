using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRental.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Helper – always returns a valid int userId
        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
            return userId;
        }


        // GET: Customer/CustomerDashboard
        [Authorize(Roles = "Customer")]

        [HttpGet]
        public async Task<IActionResult> CustomerDashboard()
        {
            return View("CustomerDashboard");
        }

       


        // GET: Customer/MyBookings
        [Authorize(Roles = "Customer")]
        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            return View();
        }


        
        // GET: Customer/CreateProfile
        [Authorize(Roles = "Customer")]

        [HttpGet]
        public IActionResult CreateProfile()
        {
            var userId = GetUserId();
            var existingCustomer = _context.Customer.FirstOrDefault(c => c.UserId == userId);

            if (existingCustomer != null)
            {
                // Already has a profile → go to edit
                return RedirectToAction("EditProfile");
            }

            var model = new Customer
            {
                UserId = userId,
                Email = User.Identity?.Name
            };

            return View(model);
        }

        // POST: Customer/CreateProfile
        [Authorize(Roles = "Customer")]

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProfile(Customer model, IFormFile NicImage, IFormFile DLImage)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = GetUserId();
            model.UserId = userId;

            // Save NIC image
            if (NicImage != null && NicImage.Length > 0)
            {
                var nicPath = Path.Combine("wwwroot/uploads", Guid.NewGuid() + Path.GetExtension(NicImage.FileName));
                Directory.CreateDirectory(Path.GetDirectoryName(nicPath)!);

                using (var stream = new FileStream(nicPath, FileMode.Create))
                {
                    await NicImage.CopyToAsync(stream);
                }
                model.NicImageUrl = nicPath.Replace("wwwroot", "");
            }

            // Save DL image
            if (DLImage != null && DLImage.Length > 0)
            {
                var dlPath = Path.Combine("wwwroot/uploads", Guid.NewGuid() + Path.GetExtension(DLImage.FileName));
                Directory.CreateDirectory(Path.GetDirectoryName(dlPath)!);

                using (var stream = new FileStream(dlPath, FileMode.Create))
                {
                    await DLImage.CopyToAsync(stream);
                }
                model.DLImageUrl = dlPath.Replace("wwwroot", "");
            }

            _context.Customer.Add(model);
            await _context.SaveChangesAsync();

            return RedirectToAction("CustomerDashboard");
        }
    }
}
