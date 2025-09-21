using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.AspNetCore.Mvc;


public class BannersController : Controller
{
    private readonly ApplicationDbContext _context;

    public BannersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // Show list of banners
    public IActionResult Index()
    {
        var banners = _context.Banner.ToList();
        return View(banners);
    }

    // GET: Create Banner
    public IActionResult Create()
    {
        return View();
    }

    // POST: Create Banner
    [HttpPost]
    public async Task<IActionResult> Create(IFormFile image, string title)
    {
        if (image != null)
        {
            var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/banners", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await image.CopyToAsync(stream);
            }

            var banner = new Banner
            {
                Title = title,
                ImageUrl = "/uploads/banners/" + fileName
            };

            _context.Banner.Add(banner);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // Delete Banner
    public IActionResult Delete(int id)
    {
        var banner = _context.Banner.Find(id);
        if (banner != null)
        {
            _context.Banner.Remove(banner);
            _context.SaveChanges();
        }
        return RedirectToAction("Index");
    }
}
