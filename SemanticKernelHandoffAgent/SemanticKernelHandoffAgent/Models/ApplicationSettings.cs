namespace SemanticKernelHandoffAgent.Models;

internal record ApplicationSettings(
    string ApplicationInsightsConnectionString,
    WeatherMcpConfiguration WeatherMcpConfiguration,
    EntraOauthConfiguration EntraOauthConfiguration,
    AzureOpenAIConfiguration AzureOpenAIConfiguration
);

internal record WeatherMcpConfiguration(
    string BaseUrl,
    string McpEndpoint,
    string McpAuthCallbackEndpoint
);

internal record EntraOauthConfiguration(
    string[] Scopes
);

internal record AzureOpenAIConfiguration(
    string DeploymentName,
    string Endpoint,
    string ApiKey
);
