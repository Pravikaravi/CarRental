namespace CarRental.Models
{
    public class User
    {
        public int Id { get; set; }

        // For both normal & Google login
        public string UserName { get; set; }

        // Nullable - for normal users only
        public string? Password { get; set; }

        public string Role { get; set; }

        // For Google or other external logins
        public string? Provider { get; set; }      // e.g. "Google"
        public string? ProviderKey { get; set; }   // unique key from Google
        public string? ProfilePhotoUrl { get; set; } // store Google profile pic

        public ICollection<Staff> Staffs { get; set; }

    }
}
