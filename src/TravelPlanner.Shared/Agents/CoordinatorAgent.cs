using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Shared.Services;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Coordinates the multi-agent workflow and aggregates results
/// </summary>
public class CoordinatorAgent : BaseAgent
{
    public override string AgentType => "Coordinator";
    protected override string AgentName => "Travel Planning Coordinator";
    
    protected override string Instructions => "You coordinate a travel planning team with specialized agents: Currency Converter, Weather Advisor, Local Knowledge, Itinerary Planner, and Budget Optimizer. Aggregate their information and present complete travel plans clearly.";

    public CoordinatorAgent(
        ILogger<CoordinatorAgent> logger,
        IOptions<AgentOptions> options) 
        : base(logger, options)
    {
    }
}
