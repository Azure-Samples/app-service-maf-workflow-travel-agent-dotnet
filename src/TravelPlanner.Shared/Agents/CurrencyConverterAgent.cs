using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Shared.ExternalServices;
using TravelPlanner.Shared.Services;
using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Handles currency conversion and budget allocation across different currencies
/// </summary>
public class CurrencyConverterAgent : BaseAgent
{
    private readonly ICurrencyService _currencyService;
    
    public override string AgentType => "CurrencyConverter";
    protected override string AgentName => "Currency Conversion Specialist";
    
    protected override string Instructions => "You are a currency conversion specialist. Convert budgets to local currencies, provide exchange rate information, suggest optimal currency strategies, and explain exchange fees. Help travelers understand their spending power.";

    public CurrencyConverterAgent(
        ILogger<CurrencyConverterAgent> logger,
        IOptions<AgentOptions> options,
        ICurrencyService currencyService) 
        : base(logger, options)
    {
        _currencyService = currencyService;
    }
    
    public async Task<CurrencyConversion> ConvertBudgetAsync(
        decimal budgetAmount,
        string fromCurrency,
        string toCurrency,
        CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Converting budget: {Amount} {From} to {To}", 
            budgetAmount, fromCurrency, toCurrency);
        
        var conversion = await _currencyService.ConvertAmountAsync(
            budgetAmount, fromCurrency, toCurrency, cancellationToken);
        
        return conversion;
    }
    
    public async Task<string> GetCurrencyAdviceAsync(
        decimal budgetAmount,
        string fromCurrency,
        string toCurrency,
        string destination,
        CancellationToken cancellationToken = default)
    {
        var agent = await CreateAgentAsync(cancellationToken);
        
        try
        {
            var conversion = await ConvertBudgetAsync(budgetAmount, fromCurrency, toCurrency, cancellationToken);
            
            var thread = agent.GetNewThread();
            var prompt = $@"Provide currency advice for a traveler going to {destination}:

Budget: {conversion.GetSummary()}

Please advise on:
1. Current exchange rate and what it means for their budget
2. Best practices for currency exchange (before travel vs. at destination)
3. Typical costs in {destination} to help them understand their spending power
4. Any currency-related tips or warnings for {destination}";

            var response = await agent.RunAsync(prompt, thread, cancellationToken: cancellationToken);
            
            return response.Text ?? "Unable to generate currency advice.";
        }
        finally
        {
            await DeleteAgentAsync(agent.Id, cancellationToken);
        }
    }
}
