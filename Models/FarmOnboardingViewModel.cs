using System.ComponentModel.DataAnnotations;

namespace KrishiSahayAI.Models
{
    public class FarmOnboardingViewModel
    {
        [Required]
        public string FarmerName { get; set; } = "";

        [Required]
        public string Location { get; set; } = "";

        [Required]
        public string ExperienceLevel { get; set; } = "";

        [Required]
        public double LandSize { get; set; }

        public string LandUnit { get; set; } = "Acre";

        [Required]
        public string SoilType { get; set; } = "";

        // =========================================================
        // SOIL ASSESSMENT
        // =========================================================

        [Required]
        public string SoilKnowledge { get; set; } = "";

        [Required]
        public string SoilTexture { get; set; } = "";

        [Required]
        public string SoilColour { get; set; } = "";

        [Required]
        public string Drainage { get; set; } = "";

        public string PreviousCrop { get; set; } = "";

        [Required]
        public string SoilTestStatus { get; set; } = "";

        // =========================================================
        // WATER / FARMING / BUDGET
        // =========================================================

        [Required]
        public string WaterSource { get; set; } = "";

        [Required]
        public string FarmingMethod { get; set; } = "";

        public decimal Budget { get; set; }
    }
}