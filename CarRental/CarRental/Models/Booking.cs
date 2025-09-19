using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarRental.Models
{
    public class Booking
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid CarId { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [ForeignKey("CarId")]
        public Car Car { get; set; }

        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }

        [Required]
        public DateTime PickupDate { get; set; }

        [Required]
        public DateTime ReturnDate { get; set; }

        [Required]
        public string Status { get; set; } = "Pending"; //default-pending, cancelled, On-Going, Returned, Overdue, Completed(confirmed)


        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalBookingAmount { get; set; }

    }
}
