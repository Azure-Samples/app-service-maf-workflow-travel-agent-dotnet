using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace TravelPlanner.Shared.ExternalServices;

/// <summary>
/// National Weather Service (NWS) API implementation - US locations only, no API key required
/// </summary>
public class NWSWeatherService : IWeatherService
{
    private const string NWS_API_BASE = "https://api.weather.gov";
    private const string USER_AGENT = "TravelPlanner/1.0 (https://github.com/Azure-Samples)";
    
    private readonly HttpClient _httpClient;
    private readonly ILogger<NWSWeatherService> _logger;
    
    public NWSWeatherService(HttpClient httpClient, ILogger<NWSWeatherService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // NWS requires a User-Agent header
        if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
        {
            _httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/geo+json");
        }
    }
    
    public async Task<WeatherForecast[]> GetForecastAsync(
        double latitude, 
        double longitude,
        DateTime startDate,
        int days,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Fetching NWS forecast for {Latitude}, {Longitude}", latitude, longitude);
            
            // Step 1: Get grid point data
            var pointsUrl = $"{NWS_API_BASE}/points/{latitude:F4},{longitude:F4}";
            var pointsResponse = await _httpClient.GetFromJsonAsync<NWSPointsResponse>(pointsUrl, cancellationToken);
            
            if (pointsResponse?.Properties?.Forecast == null)
            {
                _logger.LogWarning("No forecast URL found for location");
                return Array.Empty<WeatherForecast>();
            }
            
            // Step 2: Get forecast data
            var forecastUrl = pointsResponse.Properties.Forecast;
            var forecastResponse = await _httpClient.GetFromJsonAsync<NWSForecastResponse>(forecastUrl, cancellationToken);
            
            if (forecastResponse?.Properties?.Periods == null)
            {
                _logger.LogWarning("No forecast periods found");
                return Array.Empty<WeatherForecast>();
            }
            
            // Step 3: Convert to our model (limit to requested days * 2 for day/night periods)
            var forecasts = forecastResponse.Properties.Periods
                .Take(days * 2)
                .Select(p => new WeatherForecast
                {
                    Date = DateTime.Parse(p.StartTime ?? DateTime.UtcNow.ToString()),
                    Name = p.Name ?? "Unknown",
                    Temperature = p.Temperature ?? 0,
                    TemperatureUnit = p.TemperatureUnit ?? "F",
                    ShortForecast = p.ShortForecast ?? "",
                    DetailedForecast = p.DetailedForecast ?? "",
                    WindSpeed = p.WindSpeed ?? "",
                    WindDirection = p.WindDirection ?? "",
                    IsDaytime = p.IsDaytime ?? true
                })
                .ToArray();
            
            _logger.LogInformation("Successfully fetched {Count} forecast periods", forecasts.Length);
            return forecasts;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching NWS forecast");
            return Array.Empty<WeatherForecast>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching NWS forecast");
            return Array.Empty<WeatherForecast>();
        }
    }
    
    // NWS API response models
    private class NWSPointsResponse
    {
        public NWSProperties? Properties { get; set; }
    }
    
    private class NWSProperties
    {
        public string? Forecast { get; set; }
    }
    
    private class NWSForecastResponse
    {
        public NWSForecastProperties? Properties { get; set; }
    }
    
    private class NWSForecastProperties
    {
        public List<NWSPeriod>? Periods { get; set; }
    }
    
    private class NWSPeriod
    {
        public string? Name { get; set; }
        public string? StartTime { get; set; }
        public int? Temperature { get; set; }
        public string? TemperatureUnit { get; set; }
        public string? WindSpeed { get; set; }
        public string? WindDirection { get; set; }
        public string? ShortForecast { get; set; }
        public string? DetailedForecast { get; set; }
        public bool? IsDaytime { get; set; }
    }
}
