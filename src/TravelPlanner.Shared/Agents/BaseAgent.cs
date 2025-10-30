using Azure.AI.Agents.Persistent;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TravelPlanner.Shared.Services;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Base implementation for Agent Framework agents
/// </summary>
public abstract class BaseAgent : IAgent
{
    protected readonly ILogger Logger;
    protected readonly AgentOptions Options;
    protected readonly PersistentAgentsClient Client;
    
    public abstract string AgentType { get; }
    protected abstract string AgentName { get; }
    protected abstract string Instructions { get; }
    
    protected BaseAgent(
        ILogger logger,
        IOptions<AgentOptions> options)
    {
        Logger = logger;
        Options = options.Value;
        Client = new PersistentAgentsClient(
            Options.AzureOpenAIEndpoint.ToString(),
            new DefaultAzureCredential());
    }
    
    public virtual async Task<AIAgent> CreateAgentAsync(CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Creating {AgentType} agent", AgentType);
        
        var agent = await Client.CreateAIAgentAsync(
            model: Options.ModelDeploymentName,
            name: AgentName,
            instructions: Instructions,
            cancellationToken: cancellationToken);
        
        Logger.LogInformation("Created {AgentType} agent with ID {AgentId}", AgentType, agent.Id);
        return agent;
    }
    
    public virtual async Task DeleteAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Deleting {AgentType} agent {AgentId}", AgentType, agentId);
        await Client.Administration.DeleteAgentAsync(agentId, cancellationToken);
    }
}
