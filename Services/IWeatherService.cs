using KrishiSahayAI.Models;

namespace KrishiSahayAI.Services
{
    public interface IWeatherService
    {
        // Weather using a farm/location name
        Task<WeatherData?> GetWeatherAsync(
            string location);

        // Weather using browser GPS coordinates
        Task<WeatherData?> GetWeatherAsync(
            double latitude,
            double longitude);
    }
}