namespace KrishiSahayAI.Models
{
    public class CropObservation
    {
        public int Id { get; set; }

        public int FarmProfileId { get; set; }

        public string CropName { get; set; } = "";

        public string ImagePath { get; set; } = "";

        public string Observation { get; set; } = "";

        public string AiResult { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}