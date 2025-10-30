using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Shared.Models;
using TravelPlanner.Shared.Services;
using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Optimizes budget allocation and provides cost estimates
/// </summary>
public class BudgetOptimizerAgent : BaseAgent
{
    public override string AgentType => "BudgetOptimizer";
    protected override string AgentName => "Budget Optimization Specialist";
    
    protected override string Instructions => "You are a travel budget optimization expert. Allocate budgets across accommodation, food, activities, and transport. Provide realistic cost estimates, suggest cost-saving strategies, identify low-cost alternatives, and always include an emergency fund.";

    public BudgetOptimizerAgent(
        ILogger<BudgetOptimizerAgent> logger,
        IOptions<AgentOptions> options) 
        : base(logger, options)
    {
    }
    
    public async Task<BudgetBreakdown> OptimizeBudgetAsync(
        decimal totalBudget,
        int days,
        string destination,
        string travelStyle,
        string itinerarySummary,
        CancellationToken cancellationToken = default)
    {
        var agent = await CreateAgentAsync(cancellationToken);
        
        try
        {
            var thread = agent.GetNewThread();
            var prompt = $@"Optimize the budget allocation for a {days}-day trip to {destination}:

BUDGET: ${totalBudget:N0} USD
TRAVEL STYLE: {travelStyle}
DURATION: {days} days

PLANNED ACTIVITIES:
{itinerarySummary}

Please provide a detailed budget breakdown:

1. BUDGET ALLOCATION (provide specific dollar amounts that total ${totalBudget:N0}):
   - Accommodation: $[amount] ([percentage]% - explain choice)
   - Food & Dining: $[amount] ([percentage]% - meals/day estimate)
   - Activities & Attractions: $[amount] ([percentage]% - based on planned activities)
   - Transportation: $[amount] ([percentage]% - flights, local transport, etc.)
   - Shopping & Souvenirs: $[amount] ([percentage]%)
   - Emergency Fund: $[amount] ([percentage]% - always include 5-10%)

2. DAILY BUDGET GUIDELINE:
   - Daily spending target: $[amount]/day
   - Per meal budget: Breakfast $[X], Lunch $[Y], Dinner $[Z]
   - Activities budget per day: $[amount]

3. COST-SAVING TIPS FOR {destination}:
   - [Specific tip 1]
   - [Specific tip 2]
   - [Specific tip 3]
   - [Specific tip 4]
   - [Specific tip 5]

4. BUDGET WARNINGS:
   - [Any seasonal pricing concerns]
   - [Hidden costs to watch for]
   - [Activities that may exceed budget]

Ensure the breakdown matches the {travelStyle} style and totals exactly ${totalBudget:N0}.";

            var response = await agent.RunAsync(prompt, thread, cancellationToken: cancellationToken);
            
            // Parse the response or use a default allocation
            // For now, return a calculated breakdown
            return AllocateBudget(totalBudget, days, travelStyle);
        }
        finally
        {
            await DeleteAgentAsync(agent.Id, cancellationToken);
        }
    }
    
    private BudgetBreakdown AllocateBudget(decimal totalBudget, int days, string travelStyle)
    {
        // Adjust percentages based on travel style
        var (accomPct, foodPct, actPct, transPct, shopPct, emergPct) = travelStyle.ToLower() switch
        {
            "luxury" => (0.40m, 0.25m, 0.20m, 0.08m, 0.05m, 0.02m),
            "budget" => (0.25m, 0.30m, 0.25m, 0.10m, 0.05m, 0.05m),
            _ => (0.35m, 0.25m, 0.20m, 0.10m, 0.05m, 0.05m) // moderate
        };
        
        return new BudgetBreakdown
        {
            TotalBudget = totalBudget,
            Accommodation = totalBudget * accomPct,
            Food = totalBudget * foodPct,
            Activities = totalBudget * actPct,
            Transportation = totalBudget * transPct,
            Shopping = totalBudget * shopPct,
            Emergency = totalBudget * emergPct
        };
    }
}
