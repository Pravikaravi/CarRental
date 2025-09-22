using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CarRentalMS.Web.Controllers
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
        public async Task<IActionResult> CustomerDashboard()
        {
            var userId = GetUserId();
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                ViewBag.TotalBookings = 0;
                ViewBag.ActiveBookings = 0;
                ViewBag.CancelledBookings = 0;
                return View("CustomerDashboard");
            }

            var bookings = await _context.Booking
                .Where(b => b.CustomerId == customer.Id)
                .ToListAsync();

            ViewBag.TotalBookings = bookings.Count;
            ViewBag.ActiveBookings = bookings.Count(b => b.Status == "Pending" || b.Status == "Confirmed" || b.Status == "On-Going");
            ViewBag.CancelledBookings = bookings.Count(b => b.Status == "Cancelled");

            return View("CustomerDashboard");
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = GetUserId();
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.UserId == userId);

            if (customer == null)
            {
                return View(new List<Booking>());
            }

            var bookings = await _context.Booking
                .Include(b => b.Car)
                .Where(b => b.CustomerId == customer.Id)
                .OrderByDescending(b => b.PickupDate)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Customer model, IFormFile? NicImage, IFormFile? DLImage)
        {
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

            // Only update editable fields
            customer.Name = model.Name;
            customer.Address = model.Address;
            customer.PhoneNumber = model.PhoneNumber;

            // Optionally update files if new files are uploaded
            if (NicImage != null && NicImage.Length > 0)
                customer.NicImageUrl = await SaveFile(NicImage);

            if (DLImage != null && DLImage.Length > 0)
                customer.DLImageUrl = await SaveFile(DLImage);

            // Keep all other non-editable fields intact (do not overwrite)
            // customer.NicNumber, DrivingLicenceNumber, DLIssueDate, DLExpiryDate remain unchanged

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profile updated successfully!";
            return View(customer);
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

        // ==================== CANCEL BOOKING ACTION ====================
        [HttpGet]
        public async Task<IActionResult> CancelBooking(Guid id)
        {
            var booking = await _context.Booking.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found!";
                return RedirectToAction("MyBookings");
            }

            booking.Status = "Cancelled";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your booking has been cancelled successfully!";
            return RedirectToAction("MyBookings");
        }
        // ===============================================================
    }
}
