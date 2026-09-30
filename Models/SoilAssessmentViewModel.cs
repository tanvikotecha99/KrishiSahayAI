namespace KrishiSahayAI.Models
{
    public class SoilAssessmentViewModel
    {
        public FarmProfile Farm { get; set; } = new();

        public SoilAssessment Assessment { get; set; } = new();

        public bool HasExistingAssessment { get; set; }

        public CropKnowledge? CropKnowledge { get; set; }
    }
}