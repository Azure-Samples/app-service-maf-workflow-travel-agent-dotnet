using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Shared.Models;
using TravelPlanner.Shared.Services;
using TravelPlanner.Shared.ExternalServices;
using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Creates detailed day-by-day travel itineraries
/// </summary>
public class ItineraryPlannerAgent : BaseAgent
{
    public override string AgentType => "ItineraryPlanner";
    protected override string AgentName => "Itinerary Planning Expert";
    
    protected override string Instructions => "You are an expert travel itinerary planner. Create detailed day-by-day plans with specific timing, realistic travel times, actual venues, meal recommendations, and weather considerations. Balance popular sites with hidden gems. Match activities to interests and travel style.";

    public ItineraryPlannerAgent(
        ILogger<ItineraryPlannerAgent> logger,
        IOptions<AgentOptions> options) 
        : base(logger, options)
    {
    }
    
    public async Task<string> CreateItineraryAsync(
        TravelPlanRequest request,
        WeatherForecast[] forecasts,
        string localKnowledge,
        CancellationToken cancellationToken = default)
    {
        var agent = await CreateAgentAsync(cancellationToken);
        
        try
        {
            var days = (request.EndDate - request.StartDate).Days + 1;
            
            var weatherSummary = forecasts.Any() 
                ? string.Join("\n", forecasts.GroupBy(f => f.Date.Date).Select(g =>
                    $"{g.Key:MMM dd}: {g.First().Temperature}°{g.First().TemperatureUnit}, {g.First().ShortForecast}"))
                : "Weather forecast not available";
            
            var thread = agent.GetNewThread();
            var prompt = $@"Create a detailed {days}-day itinerary for {request.Destination}:

TRAVEL DETAILS:
- Destination: {request.Destination}
- Dates: {request.StartDate:MMM dd} to {request.EndDate:MMM dd} ({days} days)
- Budget: ${request.Budget:N0} USD
- Interests: {string.Join(", ", request.Interests)}
- Travel Style: {request.TravelStyle}
{(string.IsNullOrEmpty(request.SpecialRequests) ? "" : $"- Special Requests: {request.SpecialRequests}")}

WEATHER FORECAST:
{weatherSummary}

LOCAL KNOWLEDGE & TIPS:
{localKnowledge}

Please create a comprehensive day-by-day itinerary with the following structure for EACH day:

DAY [X] - [Date] - [Theme/Focus]

MORNING (9:00 AM - 12:00 PM):
- Activity: [Specific venue/attraction name]
- Description: [What to do and why it's special]
- Duration: [How long to spend]
- Cost estimate: $[amount]
- Weather consideration: [Adjust for forecast if needed]

LUNCH (12:00 PM - 1:30 PM):
- Restaurant/Area: [Specific recommendation]
- Cuisine: [Type of food]
- Description: [Why this choice]
- Cost estimate: $[amount]

AFTERNOON (2:00 PM - 6:00 PM):
- Activity: [Specific venue/attraction name]
- Description: [What to do and why it's special]
- Duration: [How long to spend]
- Cost estimate: $[amount]
- Weather consideration: [Adjust for forecast if needed]

DINNER (7:00 PM - 9:00 PM):
- Restaurant/Area: [Specific recommendation]
- Cuisine: [Type of food]
- Description: [Why this choice]
- Cost estimate: $[amount]

EVENING (9:00 PM - 11:00 PM) [Optional]:
- Activity: [Nightlife, shows, or relaxation]
- Description: [What to do]
- Cost estimate: $[amount]

DAILY TIPS:
- [Transportation advice for the day]
- [Any reservations needed]
- [Weather-specific tips]
- [Time-saving suggestions]

---

Please create this detailed structure for all {days} days. Make sure:
1. Activities match their interests: {string.Join(", ", request.Interests)}
2. The {request.TravelStyle} travel style is reflected
3. Weather forecast is considered for each day
4. Budget stays within ${request.Budget:N0}
5. Include both popular sites and local experiences
6. Timing is realistic with travel time between locations";

            var response = await agent.RunAsync(prompt, thread, cancellationToken: cancellationToken);
            
            return response.Text ?? "Unable to generate itinerary.";
        }
        finally
        {
            await DeleteAgentAsync(agent.Id, cancellationToken);
        }
    }
}
