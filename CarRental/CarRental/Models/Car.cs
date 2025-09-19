namespace CarRental.Models;
using System.ComponentModel.DataAnnotations;
public class Car
{
 

    [Key]
    public Guid Id { get; set; }

    [Required]
    public string CarName { get; set; }

    [Required]
    public string CarBrand { get; set; }

    [Required]
    public string CarColor { get; set; }

    [Required]
    public string CarNumber { get; set; }

    [Required]
    public int CarSeats { get; set; }

    [Required]
    public float CarRentalAmount { get; set; }

    public string CarStatus { get; set; } = "pending"; 

    public string? ImageUrl { get; set; }

    public bool IsAvailable { get; set; }
}
