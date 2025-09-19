namespace CarRental.Models
{
    public class Staff
    {
        public Guid Id { get; set; }  // Unique identifier

        // Foreign key
        public int UserId { get; set; }

        // Navigation property
        public User User { get; set; }

        // Personal Info
        public string Name { get; set; }
        public string Address { get; set; }
        public string EmailId { get; set; }
        public string PhoneNumber { get; set; }

        // Role & Access
        public bool IsActive { get; set; } = true;
        public DateTime DateJoined { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}
