using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Shared.Services;
using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Provides destination-specific knowledge, culture, safety, and local tips
/// </summary>
public class LocalKnowledgeAgent : BaseAgent
{
    public override string AgentType => "LocalKnowledge";
    protected override string AgentName => "Local Expert & Cultural Guide";
    
    protected override string Instructions => "You are a local knowledge expert. Provide cultural insights, safety tips, local transportation, authentic experiences, customs, tipping practices, emergency contacts, useful phrases, and common scams. Help travelers feel confident and respectful in their destination.";

    public LocalKnowledgeAgent(
        ILogger<LocalKnowledgeAgent> logger,
        IOptions<AgentOptions> options) 
        : base(logger, options)
    {
    }
    
    public async Task<string> GetLocalKnowledgeAsync(
        string destination,
        string[] interests,
        string? specialRequests,
        CancellationToken cancellationToken = default)
    {
        var agent = await CreateAgentAsync(cancellationToken);
        
        try
        {
            var thread = agent.GetNewThread();
            var prompt = $@"Provide comprehensive local knowledge for {destination}:

TRAVELER INTERESTS: {string.Join(", ", interests)}
{(string.IsNullOrEmpty(specialRequests) ? "" : $"SPECIAL REQUESTS: {specialRequests}")}

Please provide:

1. CULTURAL INSIGHTS:
   - Local customs and etiquette
   - Dress codes and cultural sensitivity
   - Tipping practices and expectations

2. SAFETY & PRACTICAL INFO:
   - General safety tips
   - Areas to be cautious of (if any)
   - Emergency numbers (police, ambulance, fire)
   - Nearest embassy/consulate information

3. TRANSPORTATION:
   - How to get around (public transit, taxis, etc.)
   - Transportation apps or cards to download
   - Typical costs

4. LOCAL FAVORITES:
   - Hidden gems and local spots
   - Authentic experiences beyond tourist areas
   - Local food specialties to try

5. COMMUNICATION:
   - Common phrases in the local language
   - English availability
   - Useful translation apps

6. PRACTICAL TIPS:
   - Best areas to stay
   - When shops/restaurants typically open/close
   - Common scams to watch out for
   - Cell phone/data options

Tailor advice to their interests: {string.Join(", ", interests)}";

            var response = await agent.RunAsync(prompt, thread, cancellationToken: cancellationToken);
            
            return response.Text ?? "Unable to generate local knowledge.";
        }
        finally
        {
            await DeleteAgentAsync(agent.Id, cancellationToken);
        }
    }
}
