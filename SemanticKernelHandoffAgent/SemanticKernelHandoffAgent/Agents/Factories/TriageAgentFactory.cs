using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using SemanticKernelHandoffAgent.Agents.Models;

namespace SemanticKernelHandoffAgent.Agents.Factories;

public class TriageAgentFactory : IAgentFactory
{
    private readonly IKernelBuilder _kernelBuilder;
    private readonly AgentConfiguration _configuration;

    public string AgentName => _configuration.Name;

    public TriageAgentFactory(IKernelBuilder kernelBuilder)
    {
        _kernelBuilder = kernelBuilder;
        _configuration = new AgentConfiguration(
            Name: "TriageAgent",
            Description: "Handle customer requests.",
            Instructions: "A agent that triages requests about the weather."
        );
    }

    public Task<ChatCompletionAgent> CreateAgentAsync()
    {
        var kernel = _kernelBuilder.Build();

        var agent = new ChatCompletionAgent
        {
            Name = _configuration.Name,
            Description = _configuration.Description,
            Instructions = _configuration.Instructions,
            Kernel = kernel
        };

        return Task.FromResult(agent);
    }
}
