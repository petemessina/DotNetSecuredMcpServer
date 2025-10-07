using Microsoft.SemanticKernel.Agents;
using SemanticKernelHandoffAgent.Agents.Factories;

namespace SemanticKernelHandoffAgent.Agents.Registries;

public class AgentRegistry
{
    private readonly Dictionary<Type, IAgentFactory> _factories = new();

    public void Register<TFactory>(TFactory factory) where TFactory : IAgentFactory
    {
        _factories[typeof(TFactory)] = factory;
    }

    public IAgentFactory GetFactory<TFactory>() where TFactory : IAgentFactory
    {
        if (!_factories.TryGetValue(typeof(TFactory), out var factory))
        {
            throw new InvalidOperationException($"Factory of type '{typeof(TFactory).Name}' is not registered.");
        }
        return factory;
    }

    public async Task<ChatCompletionAgent> GetAgentAsync<TFactory>() where TFactory : IAgentFactory
    {
        var factory = GetFactory<TFactory>();
        return await factory.CreateAgentAsync();
    }
}
