namespace KrishiSahayAI.Models
{
    public class FarmActivity
    {
        public int Id { get; set; }

        public int FarmProfileId { get; set; }

        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public DateTime ActivityDate { get; set; }

        public bool IsCompleted { get; set; }

        public string Category { get; set; } = "";
    }
}