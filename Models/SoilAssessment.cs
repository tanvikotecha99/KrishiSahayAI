namespace KrishiSahayAI.Models
{
    public class SoilAssessment
    {
        public int Id { get; set; }

        public int FarmProfileId { get; set; }

        public string SoilKnowledge { get; set; } = "";

        public string SoilTexture { get; set; } = "";

        public string SoilColour { get; set; } = "";

        public string Drainage { get; set; } = "";

        public string PreviousCrop { get; set; } = "";

        public string SoilTestStatus { get; set; } = "";

        public string Result { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}