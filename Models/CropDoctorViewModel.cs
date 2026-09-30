using Microsoft.AspNetCore.Http;

namespace KrishiSahayAI.Models
{
    public class CropDoctorViewModel
    {
        // =========================================================
        // FARM INFORMATION
        // =========================================================

        public FarmProfile Farm { get; set; } = new();
        public bool PublicMode { get; set; }

        // =========================================================
        // SELECTED CROP
        // =========================================================

        public FarmCrop? SelectedCrop { get; set; }


        public int? CropId { get; set; }


        public string CropName =>
            SelectedCrop?.CropName
            ?? Farm.Crop
            ?? "";


        // =========================================================
        // UPLOADED IMAGE
        // =========================================================

        public IFormFile? CropImage { get; set; }


        // =========================================================
        // FARMER'S OBSERVATION
        // =========================================================

        public string Observation { get; set; } = "";


        // =========================================================
        // AI ANALYSIS
        // =========================================================

        public string Analysis { get; set; } = "";

        public List<CropObservation> PreviousObservations { get; set; } = new();

        public bool HasAnalysis { get; set; }


        // =========================================================
        // ERROR
        // =========================================================

        public string ErrorMessage { get; set; } = "";
    }
}