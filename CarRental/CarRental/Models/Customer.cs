namespace CarRental.Models
{
    public class Customer
    {
        public Guid Id { get; set; }
        public int UserId { get; set; }    //foreign key
        public string Name { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }

        public string PhoneNumber { get; set; }

        public string NicNumber { get; set; }
        public string NicImageUrl { get; set; }

        public string DrivingLicenceNumber { get; set; }
        public string DLImageUrl { get; set; }

        public string DLIssueDate { get; set; }
        public string DLExpiryDate { get; set; }
       

    }
}
