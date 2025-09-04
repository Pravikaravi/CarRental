using CarRental.Models;
using CarRentalMS.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace CarRentalMS.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            // Register DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Unicomtic")));

            // Add Session support
            builder.Services.AddSession();

            var app = builder.Build();

            // Seed default user
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.Migrate(); // Apply any pending migrations

                if (!context.User.Any(u => u.UserName == "iampravika@gmail.com"))
                {
                    context.User.Add(new User
                    {
                        UserName = "iampravika@gmail.com",
                        Password = "admin123", 
                        Role = "Admin"
                    });
                    context.SaveChanges();
                }
            }

            // Configure middleware
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();

            // ✅ Use Session
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
