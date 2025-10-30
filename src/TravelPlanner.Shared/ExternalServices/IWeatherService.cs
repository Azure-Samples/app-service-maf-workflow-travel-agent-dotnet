namespace TravelPlanner.Shared.ExternalServices;

/// <summary>
/// Service for fetching weather forecasts
/// </summary>
public interface IWeatherService
{
    /// <summary>
    /// Gets weather forecast for a location (US only for NWS)
    /// </summary>
    Task<WeatherForecast[]> GetForecastAsync(
        double latitude, 
        double longitude, 
        DateTime startDate,
        int days,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Weather forecast for a specific period
/// </summary>
public class WeatherForecast
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty; // e.g., "Tonight", "Thursday"
    public int Temperature { get; set; }
    public string TemperatureUnit { get; set; } = "F";
    public string ShortForecast { get; set; } = string.Empty;
    public string DetailedForecast { get; set; } = string.Empty;
    public string WindSpeed { get; set; } = string.Empty;
    public string WindDirection { get; set; } = string.Empty;
    public bool IsDaytime { get; set; }
    
    public List<string> GetRecommendations()
    {
        var recommendations = new List<string>();
        
        // Temperature-based recommendations
        if (Temperature < 40)
        {
            recommendations.Add("Pack warm layers and a heavy jacket");
        }
        else if (Temperature < 60)
        {
            recommendations.Add("Bring a light jacket or sweater");
        }
        else if (Temperature > 85)
        {
            recommendations.Add("Dress in light, breathable clothing");
        }
        
        // Forecast-based recommendations
        if (ShortForecast.Contains("rain", StringComparison.OrdinalIgnoreCase) ||
            ShortForecast.Contains("shower", StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add("Don't forget an umbrella or rain jacket");
        }
        
        if (ShortForecast.Contains("snow", StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add("Waterproof boots and winter gear recommended");
        }
        
        if (ShortForecast.Contains("sunny", StringComparison.OrdinalIgnoreCase) ||
            ShortForecast.Contains("clear", StringComparison.OrdinalIgnoreCase))
        {
            recommendations.Add("Sunscreen and sunglasses recommended");
        }
        
        return recommendations;
    }
}
