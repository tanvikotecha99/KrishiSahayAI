namespace KrishiSahayAI.Models
{
    public class WaterAssessment
    {
        public int Id { get; set; }

        public int FarmProfileId { get; set; }

        public string WaterSource { get; set; } = "";

        public string WaterAvailability { get; set; } = "";

        public string IrrigationMethod { get; set; } = "";

        public string IrrigationFrequency { get; set; } = "";

        public string WaterQualityKnowledge { get; set; } = "";

        public string DrainageCondition { get; set; } = "";

        public string RecentWaterProblem { get; set; } = "";

        public string Result { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}