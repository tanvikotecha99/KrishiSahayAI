namespace KrishiSahayAI.Models
{
    public class MyCropsViewModel
    {
        public FarmProfile Farm { get; set; } = new();

        public List<FarmCrop> Crops { get; set; } = new();

        public double AllocatedArea { get; set; }

        public double RemainingArea { get; set; }

        public string AreaDisplayUnit { get; set; } = "Acre";

        public bool AreaCanBeConverted { get; set; }
    }
}