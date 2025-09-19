using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CarRental.Controllers
{
    [Authorize(Roles = "Customer")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Helper to get authenticated user's ID
        private int GetUserId()
        {
            var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(claimValue) || !int.TryParse(claimValue, out int userId))
                throw new UnauthorizedAccessException("User ID claim is missing or invalid.");
            return userId;
        }

        // GET: Booking/CreateBooking
        [HttpGet]
        public IActionResult CreateBooking()
        {
            var availableCars = _context.Cars.Where(c => c.IsAvailable).ToList();
            ViewBag.CarList = availableCars;
            return View("CreateBooking");
        }


        // POST: Booking/CreateBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(Booking booking)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CarList = _context.Cars.Where(c => c.IsAvailable).ToList();
                return View(booking);
            }

            // Get authenticated user's ID
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            // Find the customer linked to this user
            var customer = await _context.Customer.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null)
            {
                return NotFound("Customer profile not found.");
            }

            // Validate car
            var car = await _context.Cars.FindAsync(booking.CarId);
            if (car == null || !car.IsAvailable)
            {
                ModelState.AddModelError("", "Selected car is not available.");
                ViewBag.CarList = _context.Cars.Where(c => c.IsAvailable).ToList();
                return View(booking);
            }

            // Calculate total amount
            var rentalDays = (booking.ReturnDate - booking.PickupDate).Days;
            if (rentalDays <= 0)
            {
                ModelState.AddModelError("", "Return date must be after pickup date.");
                ViewBag.CarList = _context.Cars.Where(c => c.IsAvailable).ToList();
                return View(booking);
            }

            var totalAmount = rentalDays * car.CarRentalAmount;

            // Create booking
            booking.Id = Guid.NewGuid();
            booking.CustomerId = customer.Id;
            booking.Status = "Pending";
            booking.TotalBookingAmount = rentalDays * (decimal)car.CarRentalAmount;

            var bookings = await _context.Booking
                .Include(b => b.Car)
                .Where(b => b.CustomerId == customer.Id)
                .ToListAsync();

            return RedirectToAction("MyBookings", "Customer");
        }
    }
}
