namespace KrishiSahayAI.Models
{
    public class WaterAssessmentViewModel
    {
        public FarmProfile Farm { get; set; } = new();

        public WaterAssessment Assessment { get; set; } = new();

        public bool HasExistingAssessment { get; set; }

        public CropKnowledge? CropKnowledge { get; set; }
    }
}