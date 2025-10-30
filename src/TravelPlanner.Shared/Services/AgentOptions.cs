namespace TravelPlanner.Shared.Services;

public class AgentOptions
{
    public Uri AzureOpenAIEndpoint { get; set; } = null!;
    public string ModelDeploymentName { get; set; } = string.Empty;
}
