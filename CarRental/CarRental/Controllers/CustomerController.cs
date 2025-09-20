using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRental.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
            return userId;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = GetUserId();
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                customer = new Customer
                {
                    UserId = userId,
                    Email = User.Identity?.Name
                };
            }

            return View(customer);
        }

        [HttpGet]
        public IActionResult CustomerDashboard()
        {
            return View("CustomerDashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Customer model, IFormFile NicImage, IFormFile DLImage)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage);
                TempData["Error"] = string.Join(" | ", errors);
                return View(model);
            }

            var userId = GetUserId();
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                // New profile
                customer = new Customer
                {
                    UserId = userId,
                    Email = User.Identity?.Name
                };
                _context.Customer.Add(customer);
            }

            // Update fields
            customer.Name = model.Name;
            customer.Address = model.Address;
            customer.PhoneNumber = model.PhoneNumber;
            customer.NicNumber = model.NicNumber;
            customer.DrivingLicenceNumber = model.DrivingLicenceNumber;
            customer.DLIssueDate = model.DLIssueDate;
            customer.DLExpiryDate = model.DLExpiryDate;

            // Save files
            if (NicImage != null && NicImage.Length > 0)
                customer.NicImageUrl = await SaveFile(NicImage);

            if (DLImage != null && DLImage.Length > 0)
                customer.DLImageUrl = await SaveFile(DLImage);

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile saved successfully!";
            return View(customer); // stay on same page
        }

        private async Task<string> SaveFile(IFormFile file)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var uploadPath = Path.Combine("wwwroot/uploads", fileName);

            Directory.CreateDirectory(Path.GetDirectoryName(uploadPath)!);

            using (var stream = new FileStream(uploadPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return "/uploads/" + fileName;
        }
    }
}
