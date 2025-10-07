using Microsoft.SemanticKernel.Agents;

namespace SemanticKernelHandoffAgent.Agents.Factories;

public interface IAgentFactory
{
    Task<ChatCompletionAgent> CreateAgentAsync();
    string AgentName { get; }
}
