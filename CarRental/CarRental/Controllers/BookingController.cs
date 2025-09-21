using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CarRentalMS.Web.Controllers
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

        // POST: Check if customer exists (for popup booking)
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CheckExistingCustomer([FromBody] JsonElement data)
        {
            try
            {
                // Get user email from the request or session
                string userEmail = "";
                
                // Try to get email from request body first
                if (data.TryGetProperty("email", out var emailElement))
                {
                    userEmail = emailElement.GetString();
                }
                
                // If no email in request, try to get from current user session
                if (string.IsNullOrEmpty(userEmail) && User.Identity?.IsAuthenticated == true)
                {
                    userEmail = User.Identity.Name;
                }
                
                // Check if customer exists with this email
                if (!string.IsNullOrEmpty(userEmail))
                {
                    var existingCustomer = await _context.Customer
                        .FirstOrDefaultAsync(c => c.Email == userEmail);
                    
                    if (existingCustomer != null)
                    {
                        // Check if customer has complete data (not just email)
                        // If Name, PhoneNumber, Address, NicNumber, DrivingLicenceNumber are not null/empty, it's an existing customer
                        bool hasCompleteData = !string.IsNullOrEmpty(existingCustomer.Name) &&
                                             !string.IsNullOrEmpty(existingCustomer.PhoneNumber) &&
                                             !string.IsNullOrEmpty(existingCustomer.Address) &&
                                             !string.IsNullOrEmpty(existingCustomer.NicNumber) &&
                                             !string.IsNullOrEmpty(existingCustomer.DrivingLicenceNumber);
                        
                        if (hasCompleteData)
                        {
                            return Json(new { 
                                exists = true, 
                                customerId = existingCustomer.Id.ToString(),
                                isNewUser = false 
                            });
                        }
                        else
                        {
                            // Customer exists but has incomplete data - treat as new user
                            return Json(new { 
                                exists = false, 
                                customerId = "",
                                isNewUser = true,
                                email = userEmail
                            });
                        }
                    }
                    else
                    {
                        // No customer record exists - definitely a new user
                        return Json(new { 
                            exists = false, 
                            customerId = "",
                            isNewUser = true,
                            email = userEmail
                        });
                    }
                }
                
                return Json(new { exists = false, customerId = "", isNewUser = true });
            }
            catch (Exception ex)
            {
                return Json(new { exists = false, customerId = "", error = ex.Message });
            }
        }

        // POST: Create booking from popup for existing customer
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateBookingFromPopup([FromBody] JsonElement data)
        {
            try
            {
                // Parse the JSON data
                var carId = data.GetProperty("carId").GetString();
                var customerId = data.GetProperty("customerId").GetString();
                var pickupDate = data.GetProperty("pickupDate").GetString();
                var returnDate = data.GetProperty("returnDate").GetString();
                var totalAmount = data.GetProperty("totalBookingAmount").GetString();

                // Validate required fields
                if (string.IsNullOrEmpty(carId) || string.IsNullOrEmpty(customerId) || 
                    string.IsNullOrEmpty(pickupDate) || string.IsNullOrEmpty(returnDate) || 
                    string.IsNullOrEmpty(totalAmount))
                {
                    return BadRequest(new { success = false, message = "All booking details are required." });
                }

                // Validate dates
                if (!DateTime.TryParse(pickupDate, out DateTime pickup) || !DateTime.TryParse(returnDate, out DateTime returnDateObj))
                {
                    return BadRequest(new { success = false, message = "Invalid date format." });
                }

                if (returnDateObj <= pickup)
                {
                    return BadRequest(new { success = false, message = "Return date must be after pickup date." });
                }

                if (pickup < DateTime.Today)
                {
                    return BadRequest(new { success = false, message = "Pickup date cannot be in the past." });
                }

                // Validate total amount
                if (!decimal.TryParse(totalAmount, out decimal amount) || amount <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid total amount." });
                }

                // Validate GUIDs
                if (!Guid.TryParse(carId, out Guid carGuid) || !Guid.TryParse(customerId, out Guid customerGuid))
                {
                    return BadRequest(new { success = false, message = "Invalid car or customer ID." });
                }

                // Validate car exists and is available
                var car = await _context.Cars.FindAsync(carGuid);
                if (car == null || !car.IsAvailable)
                {
                    return BadRequest(new { success = false, message = "Selected car is not available." });
                }

                // Check for overlapping bookings
                var overlappingBookingExisting = await _context.Booking
                    .AnyAsync(b => b.CarId == carGuid && b.Status != "Cancelled" &&
                        ((pickup >= b.PickupDate && pickup < b.ReturnDate) ||
                         (returnDateObj > b.PickupDate && returnDateObj <= b.ReturnDate) ||
                         (pickup <= b.PickupDate && returnDateObj >= b.ReturnDate)));

                if (overlappingBookingExisting)
                {
                    return BadRequest(new { success = false, message = "Car is not available for the selected dates." });
                }

                // Validate customer exists
                var customer = await _context.Customer.FindAsync(customerGuid);
                if (customer == null)
                {
                    return BadRequest(new { success = false, message = "Customer not found." });
                }

                // Check for overlapping bookings
                var overlappingBooking = await _context.Booking
                    .AnyAsync(b => b.CarId == carGuid && b.Status != "Cancelled" &&
                        ((pickup >= b.PickupDate && pickup < b.ReturnDate) ||
                         (returnDateObj > b.PickupDate && returnDateObj <= b.ReturnDate) ||
                         (pickup <= b.PickupDate && returnDateObj >= b.ReturnDate)));

                if (overlappingBooking)
                {
                    return BadRequest(new { success = false, message = "Car is not available for the selected dates." });
                }

                // Create booking
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    CarId = carGuid,
                    CustomerId = customerGuid,
                    PickupDate = pickup,
                    ReturnDate = returnDateObj,
                    TotalBookingAmount = amount,
                    Status = "Pending"
                };

                _context.Booking.Add(booking);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Booking created successfully", bookingId = booking.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // POST: Create booking with new customer
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateBookingWithNewCustomer(IFormCollection form, IFormFile nicImage, IFormFile dlImage)
        {
            try
            {
                // Parse the form data
                var carId = form["carId"].ToString();
                var pickupDate = form["pickupDate"].ToString();
                var returnDate = form["returnDate"].ToString();
                var totalAmount = form["totalBookingAmount"].ToString();
                
                var userEmail = form["customerData[email]"].ToString();
                var name = form["customerData[name]"].ToString();
                var phoneNumber = form["customerData[phoneNumber]"].ToString();
                var address = form["customerData[address]"].ToString();
                var nicNumber = form["customerData[nicNumber]"].ToString();
                var drivingLicense = form["customerData[drivingLicenceNumber]"].ToString();
                var dlIssueDate = form["customerData[dlIssueDate]"].ToString();
                var dlExpiryDate = form["customerData[dlExpiryDate]"].ToString();

                // Validate booking data
                if (string.IsNullOrEmpty(carId) || string.IsNullOrEmpty(pickupDate) || 
                    string.IsNullOrEmpty(returnDate) || string.IsNullOrEmpty(totalAmount))
                {
                    return BadRequest(new { success = false, message = "All booking details are required." });
                }

                // Validate customer data
                if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(name) || 
                    string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(address) ||
                    string.IsNullOrEmpty(nicNumber) || string.IsNullOrEmpty(drivingLicense) ||
                    string.IsNullOrEmpty(dlIssueDate) || string.IsNullOrEmpty(dlExpiryDate))
                {
                    return BadRequest(new { success = false, message = "All customer details are required." });
                }

                // Validate email format
                if (!IsValidEmail(userEmail))
                {
                    return BadRequest(new { success = false, message = "Please enter a valid email address." });
                }

                // Validate phone number format
                if (!IsValidPhoneNumber(phoneNumber))
                {
                    return BadRequest(new { success = false, message = "Please enter a valid phone number." });
                }

                // Validate NIC number format (Sri Lankan)
                if (!IsValidNIC(nicNumber))
                {
                    return BadRequest(new { success = false, message = "Please enter a valid NIC number." });
                }

                // Validate dates
                if (!DateTime.TryParse(pickupDate, out DateTime pickup) || !DateTime.TryParse(returnDate, out DateTime returnDateObj))
                {
                    return BadRequest(new { success = false, message = "Invalid date format." });
                }

                if (returnDateObj <= pickup)
                {
                    return BadRequest(new { success = false, message = "Return date must be after pickup date." });
                }

                if (pickup < DateTime.Today)
                {
                    return BadRequest(new { success = false, message = "Pickup date cannot be in the past." });
                }

                // Validate license dates
                if (!DateTime.TryParse(dlIssueDate, out DateTime licenseIssue) || !DateTime.TryParse(dlExpiryDate, out DateTime licenseExpiry))
                {
                    return BadRequest(new { success = false, message = "Invalid license date format." });
                }

                if (licenseExpiry <= licenseIssue)
                {
                    return BadRequest(new { success = false, message = "License expiry date must be after issue date." });
                }

                if (licenseExpiry < DateTime.Today)
                {
                    return BadRequest(new { success = false, message = "Your driving license has expired. Please renew it before booking." });
                }

                // Validate total amount
                if (!decimal.TryParse(totalAmount, out decimal amount) || amount <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid total amount." });
                }

                // Validate GUIDs
                if (!Guid.TryParse(carId, out Guid carGuid))
                {
                    return BadRequest(new { success = false, message = "Invalid car ID." });
                }

                // Validate car exists and is available
                var car = await _context.Cars.FindAsync(carGuid);
                if (car == null || !car.IsAvailable)
                {
                    return BadRequest(new { success = false, message = "Selected car is not available." });
                }

                // Check for overlapping bookings
                var overlappingBookingNew = await _context.Booking
                    .AnyAsync(b => b.CarId == carGuid && b.Status != "Cancelled" &&
                        ((pickup >= b.PickupDate && pickup < b.ReturnDate) ||
                         (returnDateObj > b.PickupDate && returnDateObj <= b.ReturnDate) ||
                         (pickup <= b.PickupDate && returnDateObj >= b.ReturnDate)));

                if (overlappingBookingNew)
                {
                    return BadRequest(new { success = false, message = "Car is not available for the selected dates." });
                }

                // First create a User record
                var existingUser = await _context.User.FirstOrDefaultAsync(u => u.UserName == userEmail);
                User user;
                
                if (existingUser == null)
                {
                    user = new User
                    {
                        UserName = userEmail,
                        Role = "Customer",
                        Provider = null,
                        ProviderKey = null,
                        ProfilePhotoUrl = null
                    };
                    _context.User.Add(user);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    user = existingUser;
                }

                // Handle image uploads
                string nicImageUrl = null;
                string dlImageUrl = null;

                if (nicImage != null && nicImage.Length > 0)
                {
                    nicImageUrl = await SaveFile(nicImage);
                }

                if (dlImage != null && dlImage.Length > 0)
                {
                    dlImageUrl = await SaveFile(dlImage);
                }

                // Check if customer already exists (might have incomplete data)
                var existingCustomer = await _context.Customer.FirstOrDefaultAsync(c => c.Email == userEmail);
                Customer customer;
                
                if (existingCustomer == null)
                {
                    // Create new customer
                    customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        Email = userEmail,
                        Name = form["customerData[name]"].ToString(),
                        PhoneNumber = form["customerData[phoneNumber]"].ToString(),
                        Address = form["customerData[address]"].ToString(),
                        NicNumber = form["customerData[nicNumber]"].ToString(),
                        DrivingLicenceNumber = form["customerData[drivingLicenceNumber]"].ToString(),
                        DLIssueDate = form["customerData[dlIssueDate]"].ToString(),
                        DLExpiryDate = form["customerData[dlExpiryDate]"].ToString(),
                        NicImageUrl = nicImageUrl,
                        DLImageUrl = dlImageUrl
                    };
                    _context.Customer.Add(customer);
                }
                else
                {
                    // Update existing customer with complete data
                    existingCustomer.Name = form["customerData[name]"].ToString();
                    existingCustomer.PhoneNumber = form["customerData[phoneNumber]"].ToString();
                    existingCustomer.Address = form["customerData[address]"].ToString();
                    existingCustomer.NicNumber = form["customerData[nicNumber]"].ToString();
                    existingCustomer.DrivingLicenceNumber = form["customerData[drivingLicenceNumber]"].ToString();
                    existingCustomer.DLIssueDate = form["customerData[dlIssueDate]"].ToString();
                    existingCustomer.DLExpiryDate = form["customerData[dlExpiryDate]"].ToString();
                    
                    // Update images only if new ones were uploaded
                    if (nicImageUrl != null) existingCustomer.NicImageUrl = nicImageUrl;
                    if (dlImageUrl != null) existingCustomer.DLImageUrl = dlImageUrl;
                    
                    customer = existingCustomer;
                }

                await _context.SaveChangesAsync();

                // Create booking
                var booking = new Booking
                {
                    Id = Guid.NewGuid(),
                    CarId = Guid.Parse(carId),
                    CustomerId = customer.Id,
                    PickupDate = DateTime.Parse(pickupDate),
                    ReturnDate = DateTime.Parse(returnDate),
                    TotalBookingAmount = decimal.Parse(totalAmount),
                    Status = "Pending"
                };

                _context.Booking.Add(booking);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Booking created successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
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

        // Validation helper methods
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhoneNumber(string phoneNumber)
        {
            // Remove all non-digit characters
            var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return digitsOnly.Length >= 10 && digitsOnly.Length <= 15;
        }

        private bool IsValidNIC(string nicNumber)
        {
            if (string.IsNullOrEmpty(nicNumber))
                return false;

            // Remove spaces and convert to uppercase
            nicNumber = nicNumber.Replace(" ", "").ToUpper();

            // Old NIC format: 9 digits + V or X (e.g., 123456789V)
            if (nicNumber.Length == 10 && nicNumber.EndsWith("V") || nicNumber.EndsWith("X"))
            {
                return nicNumber.Substring(0, 9).All(char.IsDigit);
            }

            // New NIC format: 12 digits (e.g., 123456789012)
            if (nicNumber.Length == 12)
            {
                return nicNumber.All(char.IsDigit);
            }

            return false;
        }
    }
}
