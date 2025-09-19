using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CarRental.Controllers
{
    public class CarController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CarController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Car
        public async Task<IActionResult> Index()
        {
            var cars = await _context.Cars.ToListAsync();
            return View(cars);
        }

        // GET: Car/Create
        
        public IActionResult Create()
        {
            return View();
        }

        // POST: Car/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Create(Car car, IFormFile CarImage)
        {
            if (ModelState.IsValid)
            {
                car.Id = Guid.NewGuid();
                car.ImageUrl = SaveImage(CarImage);

                // Default values if not provided
                if (string.IsNullOrEmpty(car.CarStatus))
                    car.CarStatus = "Pending";

                _context.Cars.Add(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(car);

        }

        // GET: Car/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();
            return View(car);
        }

        // POST: Car/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Car car, IFormFile CarImage)
        {
            if (ModelState.IsValid)
            {
                var existingCar = await _context.Cars.AsNoTracking().FirstOrDefaultAsync(c => c.Id == car.Id);
                if (existingCar == null) return NotFound();

                car.ImageUrl = CarImage != null && CarImage.Length > 0
                    ? SaveImage(CarImage)
                    : existingCar.ImageUrl;

                // Keep CarStatus if not changed
                if (string.IsNullOrEmpty(car.CarStatus))
                    car.CarStatus = existingCar.CarStatus;

                _context.Update(car);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(car);
        }

        // GET: Car/Delete/{id}
        public async Task<IActionResult> Delete(Guid id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();
            return View(car);
        }

        // POST: Car/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();

            // Delete image from disk
            if (!string.IsNullOrEmpty(car.ImageUrl))
            {
                string fullPath = Path.Combine(_env.WebRootPath, car.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.Cars.Remove(car);
            await _context.SaveChangesAsync();

            TempData["Deleted"] = true;
            return RedirectToAction(nameof(Index));
        }


        // GET: Car/Details/{id}
        public async Task<IActionResult> Details(Guid id)
        {
            var car = await _context.Cars.FindAsync(id);
            if (car == null) return NotFound();
            return View(car);
        }

        // GET: Car/Search?query=...
        public async Task<IActionResult> Search(string query)
        {
            var cars = await _context.Cars
                .Where(c => c.CarName.Contains(query) || c.CarBrand.Contains(query))
                .ToListAsync();

            return View("Index", cars);
        }

        // ===== Helper Method =====
        private string SaveImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0) return null;

            string uploads = Path.Combine(_env.WebRootPath, "uploads");
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);

            string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
            string filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }

            return "/uploads/" + fileName;
        }
    }
}