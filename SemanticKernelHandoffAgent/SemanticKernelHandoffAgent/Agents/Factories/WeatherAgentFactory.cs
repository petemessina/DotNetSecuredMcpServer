using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using SemanticKernelHandoffAgent.Agents.Models;

namespace SemanticKernelHandoffAgent.Agents.Factories
{
    public class WeatherAgentFactory : IAgentFactory
    {
        private readonly IKernelBuilder _kernelBuilder;
        private readonly AgentConfiguration _configuration;
        private readonly HttpClientTransport _mcpClientTransport;

        public string AgentName => _configuration.Name;

        public WeatherAgentFactory(
            IKernelBuilder kernelBuilder,
            HttpClient httpClient,
            HttpClientTransport mcpClientTransport
        ) {
            
            _kernelBuilder = kernelBuilder;
            _mcpClientTransport = mcpClientTransport;
            _configuration = new AgentConfiguration(
                Name: "WeatherAgent",//DI Injected or read as resource yaml files
                Description: "An agent that provides weather information.",//DI Injected or read as resource files
                Instructions: "You provide accurate weather information using the tools you have available."//DI Injected or read as resource files
            );
        }

        public async Task<ChatCompletionAgent> CreateAgentAsync()
        {
            McpClient mcpClient = await McpClient.CreateAsync(_mcpClientTransport);
            IList<McpClientTool> tools = await mcpClient.ListToolsAsync();

            var kernel = _kernelBuilder.Build();

            OpenAIPromptExecutionSettings executionSettings = new()
            {
                Temperature = 0,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(options: new() { RetainArgumentTypes = true })
            };

            kernel.Plugins.AddFromFunctions("Tools", tools.Select(aiFunction => aiFunction.AsKernelFunction()));

            var agent = new ChatCompletionAgent
            {
                Name = _configuration.Name,
                Description = _configuration.Description,
                Instructions = _configuration.Instructions,
                Kernel = kernel,
                Arguments = new KernelArguments(executionSettings)
            };

            return agent;
        }
    }
}