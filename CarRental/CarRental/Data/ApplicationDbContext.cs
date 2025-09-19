using CarRental.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace CarRentalMS.Web.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {

        }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Booking> Booking { get; set; }
        public DbSet<Customer> Customer { get; set; }
        public DbSet<Feedback> Feedback { get; set; }
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<User> User { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Staff>()
                .HasOne(s => s.User)
                .WithMany() // or .WithMany(u => u.Staffs) if you add collection to User
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }


    }
}
