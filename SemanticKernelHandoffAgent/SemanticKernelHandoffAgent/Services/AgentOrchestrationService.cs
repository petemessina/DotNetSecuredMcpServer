using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Orchestration;
using Microsoft.SemanticKernel.Agents.Orchestration.Handoff;
using Microsoft.SemanticKernel.Agents.Runtime.InProcess;
using SemanticKernelHandoffAgent.Agents.Factories;

namespace SemanticKernelHandoffAgent.Services;

public sealed class AgentOrchestrationService
{
    private readonly TriageAgentFactory _triageAgentFactory;
    private readonly WeatherAgentFactory _weatherAgentFactory;

    public AgentOrchestrationService(
        TriageAgentFactory triageAgentFactory,
        WeatherAgentFactory weatherAgentFactory)
    {
        _triageAgentFactory = triageAgentFactory;
        _weatherAgentFactory = weatherAgentFactory;
    }

    public async Task<string> InvokeOrchestrationAsync(string message)
    {
        InProcessRuntime runtime = new();

        // Strongly-typed agent creation - compile-time safety!
        ChatCompletionAgent triageAgent = await _triageAgentFactory.CreateAgentAsync();
        ChatCompletionAgent weatherAgent = await _weatherAgentFactory.CreateAgentAsync();

        HandoffOrchestration orchestration = new(OrchestrationHandoffs
            .StartWith(triageAgent)
            .Add(triageAgent, weatherAgent)
            .Add(weatherAgent, triageAgent, "Transfer to this agent if the question is about weather."),
            triageAgent,
            weatherAgent);

        await runtime.StartAsync();

        OrchestrationResult<string> results = await orchestration.InvokeAsync(message, runtime);
        string response = await results.GetValueAsync(TimeSpan.FromSeconds(300));

        return response;
    }
}