using Microsoft.Extensions.Logging;
using TravelPlanner.Shared.Agents;
using TravelPlanner.Shared.Models;
using TravelPlanner.Shared.ExternalServices;

namespace TravelPlanner.Shared.Workflows;

/// <summary>
/// Orchestrates the multi-agent travel planning workflow
/// </summary>
public class TravelPlanningWorkflow
{
    private readonly ILogger<TravelPlanningWorkflow> _logger;
    private readonly CurrencyConverterAgent _currencyAgent;
    private readonly WeatherAdvisorAgent _weatherAgent;
    private readonly LocalKnowledgeAgent _localKnowledgeAgent;
    private readonly ItineraryPlannerAgent _itineraryAgent;
    private readonly BudgetOptimizerAgent _budgetAgent;
    
    public TravelPlanningWorkflow(
        ILogger<TravelPlanningWorkflow> logger,
        CurrencyConverterAgent currencyAgent,
        WeatherAdvisorAgent weatherAgent,
        LocalKnowledgeAgent localKnowledgeAgent,
        ItineraryPlannerAgent itineraryAgent,
        BudgetOptimizerAgent budgetAgent)
    {
        _logger = logger;
        _currencyAgent = currencyAgent;
        _weatherAgent = weatherAgent;
        _localKnowledgeAgent = localKnowledgeAgent;
        _itineraryAgent = itineraryAgent;
        _budgetAgent = budgetAgent;
    }
    
    public async Task<TravelItinerary> ExecuteAsync(
        TravelPlanRequest request,
        string taskId,
        IProgress<WorkflowProgress> progress,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting multi-agent workflow for task {TaskId}", taskId);
        
        var state = new WorkflowState { TaskId = taskId };
        var days = (request.EndDate - request.StartDate).Days + 1;
        
        try
        {
            // PHASE 1: Parallel Information Gathering (10% - 40%)
            progress.Report(new WorkflowProgress(10, "Gathering destination information...", "Workflow"));
            
            var gatheringTasks = new[]
            {
                GatherCurrencyInfoAsync(request, state, progress, cancellationToken),
                GatherWeatherInfoAsync(request, state, progress, cancellationToken),
                GatherLocalKnowledgeAsync(request, state, progress, cancellationToken)
            };
            
            await Task.WhenAll(gatheringTasks);
            state.CurrentPhase = 1;
            state.MarkStepComplete("InformationGathering");
            
            // PHASE 2: Itinerary Planning (40% - 70%)
            progress.Report(new WorkflowProgress(40, "Creating personalized itinerary...", "ItineraryPlanner"));
            
            var weatherForecasts = state.GetFromContext<WeatherForecast[]>("WeatherForecasts") ?? Array.Empty<WeatherForecast>();
            var localKnowledge = state.GetFromContext<string>("LocalKnowledge") ?? "";
            
            var itinerary = await _itineraryAgent.CreateItineraryAsync(
                request, weatherForecasts, localKnowledge, cancellationToken);
            
            state.AddToContext("Itinerary", itinerary);
            state.CurrentPhase = 2;
            state.MarkStepComplete("ItineraryPlanning");
            
            // PHASE 3: Budget Optimization (70% - 85%)
            progress.Report(new WorkflowProgress(70, "Optimizing budget allocation...", "BudgetOptimizer"));
            
            var budgetBreakdown = await _budgetAgent.OptimizeBudgetAsync(
                request.Budget,
                days,
                request.Destination,
                request.TravelStyle,
                itinerary.Length > 500 ? itinerary.Substring(0, 500) + "..." : itinerary,
                cancellationToken);
            
            state.AddToContext("Budget", budgetBreakdown);
            state.CurrentPhase = 3;
            state.MarkStepComplete("BudgetOptimization");
            
            // PHASE 4: Final Assembly (85% - 100%)
            progress.Report(new WorkflowProgress(85, "Assembling complete travel plan...", "Workflow"));
            
            var result = AssembleFinalItinerary(request, taskId, state, itinerary, budgetBreakdown);
            
            state.CurrentPhase = 4;
            state.MarkStepComplete("FinalAssembly");
            
            progress.Report(new WorkflowProgress(100, "Travel plan complete!", "Workflow"));
            
            _logger.LogInformation("Multi-agent workflow completed for task {TaskId}", taskId);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in multi-agent workflow for task {TaskId}", taskId);
            throw;
        }
    }
    
    private async Task GatherCurrencyInfoAsync(
        TravelPlanRequest request,
        WorkflowState state,
        IProgress<WorkflowProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            progress.Report(new WorkflowProgress(15, "Converting budget to local currency...", "CurrencyConverter"));
            
            // Determine destination currency (simplified - in production, use a currency mapping service)
            var destinationCurrency = GetDestinationCurrency(request.Destination);
            
            if (destinationCurrency != "USD")
            {
                var conversion = await _currencyAgent.ConvertBudgetAsync(
                    request.Budget, "USD", destinationCurrency, cancellationToken);
                
                state.AddToContext("CurrencyConversion", conversion);
                
                _logger.LogInformation("Currency conversion: {Conversion}", conversion.GetSummary());
            }
            
            state.MarkStepComplete("CurrencyGathering");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error gathering currency info, continuing without it");
        }
    }
    
    private async Task GatherWeatherInfoAsync(
        TravelPlanRequest request,
        WorkflowState state,
        IProgress<WorkflowProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            progress.Report(new WorkflowProgress(20, "Fetching weather forecast...", "WeatherAdvisor"));
            
            // Get coordinates for destination (simplified - in production, use geocoding service)
            var (lat, lon) = GetDestinationCoordinates(request.Destination);
            
            if (lat != 0 && lon != 0)
            {
                var days = (request.EndDate - request.StartDate).Days + 1;
                var forecasts = await _weatherAgent.GetForecastAsync(lat, lon, request.StartDate, days, cancellationToken);
                
                state.AddToContext("WeatherForecasts", forecasts);
                
                _logger.LogInformation("Retrieved {Count} weather forecast periods", forecasts.Length);
            }
            
            state.MarkStepComplete("WeatherGathering");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error gathering weather info, continuing without it");
        }
    }
    
    private async Task GatherLocalKnowledgeAsync(
        TravelPlanRequest request,
        WorkflowState state,
        IProgress<WorkflowProgress> progress,
        CancellationToken cancellationToken)
    {
        try
        {
            progress.Report(new WorkflowProgress(25, "Gathering local knowledge and tips...", "LocalKnowledge"));
            
            var localKnowledge = await _localKnowledgeAgent.GetLocalKnowledgeAsync(
                request.Destination,
                request.Interests.ToArray(),
                request.SpecialRequests,
                cancellationToken);
            
            state.AddToContext("LocalKnowledge", localKnowledge);
            
            _logger.LogInformation("Retrieved local knowledge for {Destination}", request.Destination);
            
            state.MarkStepComplete("LocalKnowledgeGathering");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error gathering local knowledge, continuing without it");
        }
    }
    
    private TravelItinerary AssembleFinalItinerary(
        TravelPlanRequest request,
        string taskId,
        WorkflowState state,
        string itineraryText,
        BudgetBreakdown budget)
    {
        var days = (request.EndDate - request.StartDate).Days + 1;
        var weatherForecasts = state.GetFromContext<WeatherForecast[]>("WeatherForecasts");
        var currencyConversion = state.GetFromContext<CurrencyConversion>("CurrencyConversion");
        var localKnowledge = state.GetFromContext<string>("LocalKnowledge");
        
        // Build travel tips from various sources
        var travelTips = new List<string>();
        
        // Add currency tips
        if (currencyConversion != null)
        {
            travelTips.Add($"💱 {currencyConversion.GetSummary()}");
        }
        
        // Add weather-based tips
        if (weatherForecasts != null && weatherForecasts.Any())
        {
            var allRecommendations = weatherForecasts
                .SelectMany(f => f.GetRecommendations())
                .Distinct()
                .Take(3);
            travelTips.AddRange(allRecommendations.Select(r => $"☀️ {r}"));
        }
        
        // Add a few general tips
        travelTips.Add("📱 Download offline maps of your destination");
        travelTips.Add("💳 Notify your bank of travel dates to avoid card issues");
        travelTips.Add("📋 Keep copies of important documents (passport, insurance)");
        
        // Create packing list based on weather
        var packingList = new List<string>
        {
            "Passport and travel documents",
            "Phone charger and power adapter",
            "Comfortable walking shoes",
            "Reusable water bottle",
            "Basic first aid kit"
        };
        
        if (weatherForecasts != null && weatherForecasts.Any())
        {
            var avgTemp = weatherForecasts.Average(f => f.Temperature);
            if (avgTemp < 50)
            {
                packingList.Add("Warm jacket and layers");
                packingList.Add("Cold weather accessories (hat, gloves)");
            }
            else if (avgTemp > 75)
            {
                packingList.Add("Sunscreen and sunglasses");
                packingList.Add("Light, breathable clothing");
            }
            
            if (weatherForecasts.Any(f => f.ShortForecast.Contains("rain", StringComparison.OrdinalIgnoreCase)))
            {
                packingList.Add("Umbrella or rain jacket");
            }
        }
        
        // Add interest-specific items
        if (request.Interests.Any(i => i.Contains("hiking", StringComparison.OrdinalIgnoreCase)))
        {
            packingList.Add("Hiking boots and daypack");
        }
        
        return new TravelItinerary
        {
            TaskId = taskId,
            Destination = request.Destination,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            DailyPlans = new List<DayPlan>
            {
                // Store the full itinerary text in a single day plan for now
                // In production, you'd parse the itinerary text into structured days
                new DayPlan
                {
                    DayNumber = 1,
                    Date = request.StartDate,
                    Theme = $"{days}-Day {request.Destination} Itinerary",
                    Morning = new Activity
                    {
                        Location = request.Destination,
                        Description = itineraryText,
                        EstimatedCost = 0
                    }
                }
            },
            Budget = budget,
            TravelTips = travelTips,
            PackingList = packingList,
            EmergencyContacts = new EmergencyInfo
            {
                LocalEmergencyNumber = "112 (EU) or 911 (US/Canada)",
                NearestEmbassy = $"Contact your embassy in {request.Destination}",
                HealthcareInfo = "Travel with comprehensive health insurance."
            }
        };
    }
    
    // Helper methods for destination data (simplified - in production, use proper services)
    private string GetDestinationCurrency(string destination)
    {
        // Simplified mapping - in production, use a proper currency/country database
        return destination.ToLower() switch
        {
            var d when d.Contains("paris") || d.Contains("france") => "EUR",
            var d when d.Contains("london") || d.Contains("uk") || d.Contains("england") => "GBP",
            var d when d.Contains("tokyo") || d.Contains("japan") => "JPY",
            var d when d.Contains("mexico") => "MXN",
            var d when d.Contains("canada") => "CAD",
            _ => "USD" // Default
        };
    }
    
    private (double lat, double lon) GetDestinationCoordinates(string destination)
    {
        // Simplified mapping - in production, use a geocoding service
        // NWS API only works for US locations
        return destination.ToLower() switch
        {
            var d when d.Contains("new york") => (40.7128, -74.0060),
            var d when d.Contains("los angeles") => (34.0522, -118.2437),
            var d when d.Contains("chicago") => (41.8781, -87.6298),
            var d when d.Contains("san francisco") => (37.7749, -122.4194),
            var d when d.Contains("miami") => (25.7617, -80.1918),
            var d when d.Contains("seattle") => (47.6062, -122.3321),
            var d when d.Contains("boston") => (42.3601, -71.0589),
            var d when d.Contains("washington") || d.Contains("dc") => (38.9072, -77.0369),
            _ => (0, 0) // Unknown - will be handled gracefully
        };
    }
}
