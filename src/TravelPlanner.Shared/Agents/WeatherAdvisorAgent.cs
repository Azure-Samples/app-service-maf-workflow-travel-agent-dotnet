using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Shared.ExternalServices;
using TravelPlanner.Shared.Services;
using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Provides weather forecasts and weather-based recommendations
/// </summary>
public class WeatherAdvisorAgent : BaseAgent
{
    private readonly IWeatherService _weatherService;
    
    public override string AgentType => "WeatherAdvisor";
    protected override string AgentName => "Weather & Packing Advisor";
    
    protected override string Instructions => "You are a weather and packing specialist. Analyze forecasts, provide packing recommendations, suggest activity modifications based on conditions, warn about severe weather, and recommend best times for outdoor activities.";

    public WeatherAdvisorAgent(
        ILogger<WeatherAdvisorAgent> logger,
        IOptions<AgentOptions> options,
        IWeatherService weatherService) 
        : base(logger, options)
    {
        _weatherService = weatherService;
    }
    
    public async Task<WeatherForecast[]> GetForecastAsync(
        double latitude,
        double longitude,
        DateTime startDate,
        int days,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Getting weather forecast for {Lat}, {Lon} for {Days} days", 
            latitude, longitude, days);
        
        var forecasts = await _weatherService.GetForecastAsync(
            latitude, longitude, startDate, days, cancellationToken);
        
        return forecasts;
    }
    
    public async Task<string> GetWeatherAdviceAsync(
        WeatherForecast[] forecasts,
        string destination,
        string[] interests,
        CancellationToken cancellationToken = default)
    {
        var agent = await CreateAgentAsync(cancellationToken);
        
        try
        {
            var weatherSummary = string.Join("\n\n", forecasts.Select(f => 
                $"{f.Name}: {f.Temperature}°{f.TemperatureUnit}, {f.ShortForecast}\n" +
                $"Details: {f.DetailedForecast}\n" +
                $"Wind: {f.WindSpeed} {f.WindDirection}"));
            
            var thread = agent.GetNewThread();
            var prompt = $@"Provide weather-based travel advice for {destination}:

WEATHER FORECAST:
{weatherSummary}

TRAVELER INTERESTS: {string.Join(", ", interests)}

Please provide:
1. Weather overview and what to expect
2. Detailed packing list based on these conditions
3. Activity recommendations that work well with this weather
4. Any weather-related warnings or precautions
5. Best times of day for outdoor activities

Tailor your advice to their interests: {string.Join(", ", interests)}";

            var response = await agent.RunAsync(prompt, thread, cancellationToken: cancellationToken);
            
            return response.Text ?? "Unable to generate weather advice.";
        }
        finally
        {
            await DeleteAgentAsync(agent.Id, cancellationToken);
        }
    }
}
