namespace KrishiSahayAI.Models
{
    public class WeatherData
    {
        public string LocationName { get; set; } = "";

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Timezone { get; set; } = "";

        public DateTime RetrievedAt { get; set; } =
            DateTime.UtcNow;


        // =====================================================
        // CURRENT WEATHER
        // =====================================================

        public double TemperatureC { get; set; }

        public double HumidityPercent { get; set; }

        public double WindSpeedKmh { get; set; }

        public double PrecipitationMm { get; set; }

        public int WeatherCode { get; set; }

        public bool IsDay { get; set; }


        // =====================================================
        // SIMPLE WEATHER DESCRIPTION
        // =====================================================

        public string WeatherDescription { get; set; } = "";


        // =====================================================
        // DAILY FORECAST
        // =====================================================

        public List<WeatherDay> Forecast { get; set; } = new();
    }


    public class WeatherDay
    {
        public DateTime Date { get; set; }

        public double MaxTemperatureC { get; set; }

        public double MinTemperatureC { get; set; }

        public double PrecipitationProbabilityPercent { get; set; }

        public double PrecipitationMm { get; set; }

        public int WeatherCode { get; set; }

        public string WeatherDescription { get; set; } = "";
    }
}