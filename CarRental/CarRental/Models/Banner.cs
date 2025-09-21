namespace CarRental.Models
{

    public class Banner
    {
        public int Id { get; set; }
        public string Title { get; set; } //(for caption)
        public string ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }


}
