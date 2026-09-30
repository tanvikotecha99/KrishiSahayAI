using System.Collections.Generic;

namespace KrishiSahayAI.Models
{
    public class AiChatViewModel
    {
        // =========================================================
        // SELECTED FARM
        // =========================================================

        public FarmProfile Farm { get; set; } = new();

        public int? SelectedFarmId { get; set; }


        // =========================================================
        // SELECTED CROP
        // =========================================================

        public FarmCrop? SelectedCrop { get; set; }

        public int? SelectedCropId { get; set; }


        // =========================================================
        // FARM / CROP SELECTION DATA
        // =========================================================

        public List<FarmProfile> AvailableFarms { get; set; }
            = new();

        public List<FarmCrop> AvailableCrops { get; set; }
            = new();


        // =========================================================
        // GENERAL / FARM-SPECIFIC MODE
        // =========================================================

        public bool IsGeneralQuestion { get; set; }


        // =========================================================
        // AI CHAT
        // =========================================================

        public string Question { get; set; } = "";

        public string Answer { get; set; } = "";

        public bool HasAnswer { get; set; }

        // Selected language for AI response and voice features.
        // Default remains English so existing behavior is unchanged.
        public string SelectedLanguage { get; set; } = "auto";


        // =========================================================
        // SELECTED CROP NAME
        // =========================================================

        public string CropName =>
            SelectedCrop?.CropName
            ?? "";
    }
}
