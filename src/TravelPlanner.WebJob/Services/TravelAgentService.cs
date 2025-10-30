using Microsoft.Extensions.Logging;
using TravelPlanner.Shared.Models;
using TravelPlanner.Shared.Workflows;

namespace TravelPlanner.WebJob.Services;

public class TravelAgentService : ITravelAgentService
{
    private readonly ILogger<TravelAgentService> _logger;
    private readonly TravelPlanningWorkflow _workflow;

    public TravelAgentService(
        ILogger<TravelAgentService> logger,
        TravelPlanningWorkflow workflow)
    {
        _logger = logger;
        _workflow = workflow;
    }

    public async Task<TravelItinerary> GenerateTravelPlanAsync(
        TravelPlanRequest request,
        string taskId,
        IProgress<(int percentage, string step)> progress,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting multi-agent travel plan generation for task {TaskId}", taskId);

        // Convert progress reporting format
        var workflowProgress = new Progress<WorkflowProgress>(update =>
        {
            var stepWithAgent = string.IsNullOrEmpty(update.AgentName) 
                ? update.Step 
                : $"[{update.AgentName}] {update.Step}";
            progress.Report((update.Percentage, stepWithAgent));
        });

        // Execute the multi-agent workflow
        var itinerary = await _workflow.ExecuteAsync(
            request,
            taskId,
            workflowProgress,
            cancellationToken);

        _logger.LogInformation("Completed multi-agent travel plan generation for task {TaskId}", taskId);

        return itinerary;
    }
}
