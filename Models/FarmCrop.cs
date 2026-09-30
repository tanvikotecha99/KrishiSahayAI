namespace KrishiSahayAI.Models
{
    public class FarmCrop
    {
        public int Id { get; set; }

        // The user who owns this crop record
        public string UserId { get; set; } = "";

        // The farm this crop belongs to
        public int FarmId { get; set; }

        // Crop name, e.g. Groundnut, Wheat, Cotton
        public string CropName { get; set; } = "";

        // Optional variety
        public string Variety { get; set; } = "";

        // Optional season, e.g. Kharif, Rabi, Summer
        public string Season { get; set; } = "";

        // Optional area used for this crop
        public double Area { get; set; }

        public string AreaUnit { get; set; } = "Acre";

        // Optional sowing date
        public DateTime? SowingDate { get; set; }

        // Additional notes
        public string Notes { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}