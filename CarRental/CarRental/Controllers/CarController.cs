using CarRentalMS.Web.Data;
using CarRental.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRental.Controllers
{
    public class CarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CarController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ✅ GET: Car (List)
        public IActionResult Index()
        {
            var cars = _context.Cars.ToList();
            return View(cars);
        }

        // ✅ GET: Car/Details/{id}
        public IActionResult Details(Guid id)
        {
            var car = _context.Cars.FirstOrDefault(c => c.Id == id);
            if (car == null) return NotFound();
            return View(car);
        }

        // ✅ GET: Car/Create
        public IActionResult Create()
        {
            return View();
        }

        // ✅ POST: Car/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Car car, IFormFile CarImage)
        {
            if (ModelState.IsValid)
            {
                car.Id = Guid.NewGuid();

                if (CarImage != null && CarImage.Length > 0)
                {
                    string uploadDir = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(CarImage.FileName);
                    string filePath = Path.Combine(uploadDir, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await CarImage.CopyToAsync(fileStream);
                    }

                    car.ImageUrl = "/uploads/" + fileName;
                }

                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(car);
        }

        // ✅ GET: Car/Edit/{id}
        public IActionResult Edit(Guid id)
        {
            var car = _context.Cars.FirstOrDefault(c => c.Id == id);
            if (car == null) return NotFound();
            return View(car);
        }

        // ✅ POST: Car/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, Car updatedCar, IFormFile CarImage)
        {
            var car = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound();

            if (ModelState.IsValid)
            {
                updatedCar.Id = id;

                if (CarImage != null && CarImage.Length > 0)
                {
                    string uploadDir = Path.Combine(_environment.WebRootPath, "uploads");
                    if (!Directory.Exists(uploadDir))
                        Directory.CreateDirectory(uploadDir);

                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(CarImage.FileName);
                    string filePath = Path.Combine(uploadDir, fileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await CarImage.CopyToAsync(fileStream);
                    }

                    if (!string.IsNullOrEmpty(car.ImageUrl))
                    {
                        string oldFile = Path.Combine(_environment.WebRootPath, car.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldFile))
                        {
                            System.IO.File.Delete(oldFile);
                        }
                    }

                    updatedCar.ImageUrl = "/uploads/" + fileName;
                }
                else
                {
                    updatedCar.ImageUrl = car.ImageUrl;
                }

                _context.Update(updatedCar);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(updatedCar);
        }

        // ✅ GET: Car/Delete/{id}
        public IActionResult Delete(Guid id)
        {
            var car = _context.Cars.FirstOrDefault(c => c.Id == id);
            if (car == null) return NotFound();
            return View(car);
        }

        // ✅ POST: Car/DeleteConfirmed
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var car = await _context.Cars.FirstOrDefaultAsync(c => c.Id == id);
            if (car == null) return NotFound();

            if (!string.IsNullOrEmpty(car.ImageUrl))
            {
                string oldFile = Path.Combine(_environment.WebRootPath, car.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(oldFile))
                {
                    System.IO.File.Delete(oldFile);
                }
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
