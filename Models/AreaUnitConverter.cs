namespace KrishiSahayAI.Models
{
    /// <summary>
    /// Converts supported agricultural area units to acres.
    /// Bigha is intentionally not converted because its size varies by region.
    /// </summary>
    public static class AreaUnitConverter
    {
        public static bool TryToAcres(
            double value,
            string? unit,
            out double acres)
        {
            acres = 0;

            if (value < 0 || string.IsNullOrWhiteSpace(unit))
                return false;

            switch (unit.Trim().ToLowerInvariant())
            {
                case "acre":
                case "acres":
                    acres = value;
                    return true;

                case "hectare":
                case "hectares":
                    acres = value * 2.47105381;
                    return true;

                case "guntha":
                case "gunthas":
                    acres = value * 0.025;
                    return true;

                case "square meter":
                case "square meters":
                case "sqm":
                    acres = value / 4046.8564224;
                    return true;

                case "square feet":
                case "square foot":
                case "sq ft":
                case "sqft":
                    acres = value / 43560.0;
                    return true;

                default:
                    // Bigha is not converted because its value
                    // varies by region.
                    return false;
            }
        }

        public static bool IsSameUnit(
            string? first,
            string? second)
        {
            return string.Equals(
                Normalize(first),
                Normalize(second),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string Normalize(string? unit)
        {
            return (unit ?? "")
                .Trim()
                .ToLowerInvariant() switch
            {
                "acres" => "acre",
                "hectares" => "hectare",
                "gunthas" => "guntha",
                "square meters" => "square meter",
                "square foot" => "square feet",
                "sq ft" => "square feet",
                "sqft" => "square feet",
                _ => (unit ?? "")
                    .Trim()
                    .ToLowerInvariant()
            };
        }
    }
}