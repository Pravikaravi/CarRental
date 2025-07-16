namespace CarRental.Models
{
    public class Feedback
    {
        public Guid Id { get; set; }

        public int UserId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string UserFeedback { get; set; }

    }
}
