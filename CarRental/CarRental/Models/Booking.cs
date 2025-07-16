namespace CarRental.Models
{
    public class Booking
    {
        public Guid Id { get; set; }
        public int CarId { get; set; }
        public int CustomerId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string PickupDate { get; set; }
        public string ReturnDate { get; set; }
    }
}
