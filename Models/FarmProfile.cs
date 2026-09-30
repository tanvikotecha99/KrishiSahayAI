namespace KrishiSahayAI.Models
{
    public class FarmProfile
    {
        public int Id { get; set; }

        public string UserId { get; set; } = "";

        public string FarmerName { get; set; } = "";

        public string Location { get; set; } = "";

        public double LandSize { get; set; }

        public string LandUnit { get; set; } = "Acre";

        public string SoilType { get; set; } = "";

        public string WaterSource { get; set; } = "";

        public string FarmingMethod { get; set; } = "";

        public string Crop { get; set; } = "";

        public string ExperienceLevel { get; set; } = "";

        public decimal Budget { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}