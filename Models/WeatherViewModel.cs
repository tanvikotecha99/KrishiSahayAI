using KrishiSahayAI.Models;

namespace KrishiSahayAI.Models
{
    public class WeatherViewModel
    {
        public FarmProfile Farm { get; set; } = new();

        public WeatherData? Weather { get; set; }

        public CropKnowledge? CropKnowledge { get; set; }

        public bool HasWeather { get; set; }

        public string ErrorMessage { get; set; } = "";
    }
}