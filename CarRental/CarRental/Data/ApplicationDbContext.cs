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
        public DbSet<Staff> Staff { get; set; }
        public DbSet<User> User { get; set; }

    }
}
