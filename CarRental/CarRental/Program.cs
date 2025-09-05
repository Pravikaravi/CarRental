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

            // Add services to the container
            builder.Services.AddControllersWithViews();

            // Register DbContext
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("Unicomtic")));

<<<<<<< HEAD
            // 🔥 Add session support
            builder.Services.AddDistributedMemoryCache(); // in-memory session storage
            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30); // session timeout
                options.Cookie.HttpOnly = true;                // more secure
                options.Cookie.IsEssential = true;             // required for GDPR compliance
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
=======
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
>>>>>>> origin/main
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();

<<<<<<< HEAD
            // 🔥 Add session middleware BEFORE UseAuthorization
=======
            // ✅ Use Session
>>>>>>> origin/main
            app.UseSession();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }


    }
}
