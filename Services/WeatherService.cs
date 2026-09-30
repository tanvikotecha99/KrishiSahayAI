using System.Text.Json;
using KrishiSahayAI.Models;

namespace KrishiSahayAI.Services
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;

        private const string GeocodingEndpoint =
            "https://geocoding-api.open-meteo.com/v1/search";

        private const string ForecastEndpoint =
            "https://api.open-meteo.com/v1/forecast";

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.Timeout =
                TimeSpan.FromSeconds(15);
        }

        // =========================================================
        // WEATHER USING FARM / LOCATION NAME
        // =========================================================

        public async Task<WeatherData?> GetWeatherAsync(
            string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                return null;
            }

            // =====================================================
            // STEP 1 — FIND LOCATION COORDINATES
            // =====================================================

            var encodedLocation =
                Uri.EscapeDataString(location.Trim());

            var geocodingUrl =
                $"{GeocodingEndpoint}" +
                $"?name={encodedLocation}" +
                "&count=1" +
                "&language=en" +
                "&format=json";

            try
            {
                using var geoResponse =
                    await _httpClient.GetAsync(
                        geocodingUrl);

                if (!geoResponse.IsSuccessStatusCode)
                {
                    return null;
                }

                var geoJson =
                    await geoResponse.Content
                        .ReadAsStringAsync();

                using var geoDocument =
                    JsonDocument.Parse(geoJson);

                var geoRoot =
                    geoDocument.RootElement;

                if (!geoRoot.TryGetProperty(
                        "results",
                        out var results))
                {
                    return null;
                }

                if (results.GetArrayLength() == 0)
                {
                    return null;
                }

                var firstResult =
                    results[0];

                var latitude =
                    firstResult
                        .GetProperty("latitude")
                        .GetDouble();

                var longitude =
                    firstResult
                        .GetProperty("longitude")
                        .GetDouble();

                var locationName =
                    firstResult
                        .GetProperty("name")
                        .GetString()
                        ?? location;

                var timezone =
                    firstResult.TryGetProperty(
                        "timezone",
                        out var timezoneProperty)
                        ? timezoneProperty
                            .GetString()
                            ?? "auto"
                        : "auto";

                // Use the common weather method
                return await GetWeatherByCoordinatesAsync(
                    latitude,
                    longitude,
                    locationName,
                    timezone);
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // WEATHER USING BROWSER GPS COORDINATES
        // =========================================================

        public async Task<WeatherData?> GetWeatherAsync(
            double latitude,
            double longitude)
        {
            // Basic coordinate validation
            if (latitude < -90 ||
                latitude > 90 ||
                longitude < -180 ||
                longitude > 180)
            {
                return null;
            }

            return await GetWeatherByCoordinatesAsync(
                latitude,
                longitude,
                "Current Location",
                "auto");
        }

        // =========================================================
        // COMMON OPEN-METEO WEATHER REQUEST
        // =========================================================

        private async Task<WeatherData?> GetWeatherByCoordinatesAsync(
            double latitude,
            double longitude,
            string locationName,
            string timezone)
        {
            var forecastUrl =
                $"{ForecastEndpoint}" +
                $"?latitude={latitude}" +
                $"&longitude={longitude}" +
                "&current=" +
                "temperature_2m," +
                "relative_humidity_2m," +
                "precipitation," +
                "weather_code," +
                "wind_speed_10m," +
                "is_day" +
                "&daily=" +
                "weather_code," +
                "temperature_2m_max," +
                "temperature_2m_min," +
                "precipitation_sum," +
                "precipitation_probability_max" +
                "&forecast_days=7" +
                $"&timezone={Uri.EscapeDataString(timezone)}";

            try
            {
                using var weatherResponse =
                    await _httpClient.GetAsync(
                        forecastUrl);

                if (!weatherResponse.IsSuccessStatusCode)
                {
                    return null;
                }

                var weatherJson =
                    await weatherResponse.Content
                        .ReadAsStringAsync();

                using var weatherDocument =
                    JsonDocument.Parse(weatherJson);

                var weatherRoot =
                    weatherDocument.RootElement;

                if (!weatherRoot.TryGetProperty(
                        "current",
                        out var current))
                {
                    return null;
                }

                // =================================================
                // READ CURRENT WEATHER
                // =================================================

                var temperature =
                    GetDouble(
                        current,
                        "temperature_2m");

                var humidity =
                    GetDouble(
                        current,
                        "relative_humidity_2m");

                var windSpeed =
                    GetDouble(
                        current,
                        "wind_speed_10m");

                var precipitation =
                    GetDouble(
                        current,
                        "precipitation");

                var weatherCode =
                    GetInt(
                        current,
                        "weather_code");

                var isDay =
                    GetInt(
                        current,
                        "is_day") == 1;

                // =================================================
                // CREATE WEATHER OBJECT
                // =================================================

                var weather = new WeatherData
                {
                    LocationName =
                        locationName,

                    Latitude =
                        latitude,

                    Longitude =
                        longitude,

                    Timezone =
                        timezone,

                    RetrievedAt =
                        DateTime.UtcNow,

                    TemperatureC =
                        temperature,

                    HumidityPercent =
                        humidity,

                    WindSpeedKmh =
                        windSpeed,

                    PrecipitationMm =
                        precipitation,

                    WeatherCode =
                        weatherCode,

                    IsDay =
                        isDay,

                    WeatherDescription =
                        GetWeatherDescription(
                            weatherCode)
                };

                // =================================================
                // READ 7-DAY FORECAST
                // =================================================

                if (weatherRoot.TryGetProperty(
                        "daily",
                        out var daily))
                {
                    var dates =
                        GetStringArray(
                            daily,
                            "time");

                    var maxTemperatures =
                        GetDoubleArray(
                            daily,
                            "temperature_2m_max");

                    var minTemperatures =
                        GetDoubleArray(
                            daily,
                            "temperature_2m_min");

                    var precipitationProbabilities =
                        GetDoubleArray(
                            daily,
                            "precipitation_probability_max");

                    var precipitationTotals =
                        GetDoubleArray(
                            daily,
                            "precipitation_sum");

                    var dailyWeatherCodes =
                        GetIntArray(
                            daily,
                            "weather_code");

                    var count =
                        dates.Count;

                    for (int i = 0;
                         i < count;
                         i++)
                    {
                        if (!DateTime.TryParse(
                                dates[i],
                                out var date))
                        {
                            continue;
                        }

                        var day =
                            new WeatherDay
                            {
                                Date =
                                    date,

                                MaxTemperatureC =
                                    GetArrayValue(
                                        maxTemperatures,
                                        i),

                                MinTemperatureC =
                                    GetArrayValue(
                                        minTemperatures,
                                        i),

                                PrecipitationProbabilityPercent =
                                    GetArrayValue(
                                        precipitationProbabilities,
                                        i),

                                PrecipitationMm =
                                    GetArrayValue(
                                        precipitationTotals,
                                        i),

                                WeatherCode =
                                    GetArrayValue(
                                        dailyWeatherCodes,
                                        i),

                                WeatherDescription =
                                    GetWeatherDescription(
                                        GetArrayValue(
                                            dailyWeatherCodes,
                                            i))
                            };

                        weather.Forecast.Add(day);
                    }
                }

                return weather;
            }
            catch
            {
                return null;
            }
        }

        // =========================================================
        // JSON HELPERS
        // =========================================================

        private static double GetDouble(
            JsonElement element,
            string property)
        {
            if (element.TryGetProperty(
                    property,
                    out var value))
            {
                return value.GetDouble();
            }

            return 0;
        }

        private static int GetInt(
            JsonElement element,
            string property)
        {
            if (element.TryGetProperty(
                    property,
                    out var value))
            {
                return value.GetInt32();
            }

            return 0;
        }

        private static List<string> GetStringArray(
            JsonElement element,
            string property)
        {
            var values =
                new List<string>();

            if (!element.TryGetProperty(
                    property,
                    out var array))
            {
                return values;
            }

            foreach (var item in array.EnumerateArray())
            {
                values.Add(
                    item.GetString() ?? "");
            }

            return values;
        }

        private static List<double> GetDoubleArray(
            JsonElement element,
            string property)
        {
            var values =
                new List<double>();

            if (!element.TryGetProperty(
                    property,
                    out var array))
            {
                return values;
            }

            foreach (var item in array.EnumerateArray())
            {
                values.Add(
                    item.GetDouble());
            }

            return values;
        }

        private static List<int> GetIntArray(
            JsonElement element,
            string property)
        {
            var values =
                new List<int>();

            if (!element.TryGetProperty(
                    property,
                    out var array))
            {
                return values;
            }

            foreach (var item in array.EnumerateArray())
            {
                values.Add(
                    item.GetInt32());
            }

            return values;
        }

        private static double GetArrayValue(
            List<double> values,
            int index)
        {
            if (index >= 0 &&
                index < values.Count)
            {
                return values[index];
            }

            return 0;
        }

        private static int GetArrayValue(
            List<int> values,
            int index)
        {
            if (index >= 0 &&
                index < values.Count)
            {
                return values[index];
            }

            return 0;
        }

        // =========================================================
        // WEATHER CODE DESCRIPTION
        // =========================================================

        private static string GetWeatherDescription(
            int code)
        {
            return code switch
            {
                0 =>
                    "Clear sky",

                1 or 2 or 3 =>
                    "Mainly clear, partly cloudy or overcast",

                45 or 48 =>
                    "Fog",

                51 or 53 or 55 =>
                    "Drizzle",

                56 or 57 =>
                    "Freezing drizzle",

                61 or 63 or 65 =>
                    "Rain",

                66 or 67 =>
                    "Freezing rain",

                71 or 73 or 75 =>
                    "Snow fall",

                77 =>
                    "Snow grains",

                80 or 81 or 82 =>
                    "Rain showers",

                85 or 86 =>
                    "Snow showers",

                95 =>
                    "Thunderstorm",

                96 or 99 =>
                    "Thunderstorm with hail",

                _ =>
                    "Weather conditions unavailable"
            };
        }
    }
}