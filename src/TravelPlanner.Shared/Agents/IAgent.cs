using Azure.AI.Agents.Persistent;
using Microsoft.Agents.AI;

namespace TravelPlanner.Shared.Agents;

/// <summary>
/// Base interface for all Agent Framework agents
/// </summary>
public interface IAgent
{
    /// <summary>
    /// The unique identifier for this agent type
    /// </summary>
    string AgentType { get; }
    
    /// <summary>
    /// Creates a new Agent Framework agent instance
    /// </summary>
    Task<AIAgent> CreateAgentAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cleans up agent resources
    /// </summary>
    Task DeleteAgentAsync(string agentId, CancellationToken cancellationToken = default);
}
