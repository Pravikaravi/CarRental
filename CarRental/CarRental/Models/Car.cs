namespace CarRental.Models
{
    public class Car
    {
        public Guid Id { get; set; }
        public string CarName { get; set; }

        public string ImageUrl { get; set; }

        public string CarBrand { get; set; }

        public string CarColor { get; set; }
       
        public string CarNumber{ get; set; }

        public string CarSeats { get; set; }
        public bool IsAvailable { get; set; }
        


    }
}
