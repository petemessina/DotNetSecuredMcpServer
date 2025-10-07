using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using SemanticKernelHandoffAgent.Agents.Factories;
using SemanticKernelHandoffAgent.DelegatedHandlers;
using SemanticKernelHandoffAgent.Models;
using SemanticKernelHandoffAgent.Providers;
using SemanticKernelHandoffAgent.Services;

IConfigurationRoot config = new ConfigurationBuilder()
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

var builder = WebApplication.CreateBuilder(args);
var applicationSettings = builder.Configuration
    .GetSection("ApplicationSettings")
    .Get<ApplicationSettings>();

var kernelBuilder = CreateKernelBuilder(builder.Services, applicationSettings);
var resourceBuilder = ResourceBuilder
    .CreateDefault()
    .AddService("SemanticKernelHandoffAgent");

// Enable model diagnostics with sensitive data.
AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

using var traceProvider = Sdk.CreateTracerProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddSource("Microsoft.SemanticKernel*")
    .AddAzureMonitorTraceExporter(options => options.ConnectionString = applicationSettings.ApplicationInsightsConnectionString)
    .AddAspNetCoreInstrumentation()
    .Build();

using var meterProvider = Sdk.CreateMeterProviderBuilder()
    .SetResourceBuilder(resourceBuilder)
    .AddMeter("Microsoft.SemanticKernel*")
    .AddAzureMonitorMetricExporter(options => options.ConnectionString = applicationSettings.ApplicationInsightsConnectionString)
    .AddAspNetCoreInstrumentation()
    .Build();

using var loggerFactory = LoggerFactory.Create(builder =>
{
    // Add OpenTelemetry as a logging provider
    builder.AddOpenTelemetry(options =>
    {
        options.SetResourceBuilder(resourceBuilder);
        options.AddAzureMonitorLogExporter(options => options.ConnectionString = applicationSettings.ApplicationInsightsConnectionString);
        // Format log messages. This is default to false.
        options.IncludeFormattedMessage = true;
        options.IncludeScopes = true;
    });
    builder.SetMinimumLevel(LogLevel.Information);
});

// Add authentication with Microsoft Entra ID
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAd", options);
        options.TokenValidationParameters.NameClaimType = "name";
    }, options => 
    {
        builder.Configuration.Bind("AzureAd", options);
    }).EnableTokenAcquisitionToCallDownstreamApi(options =>
    {
        // Configure the confidential client application for OBO flow
        builder.Configuration.Bind("AzureAd", options);
    })
    .AddInMemoryTokenCaches();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IKernelBuilder>(sp => kernelBuilder);
builder.Services.AddSingleton<ApplicationSettings>(sp => applicationSettings);
builder.Services.AddSingleton<TriageAgentFactory>();
builder.Services.AddHttpClient<WeatherAgentFactory>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<DownstreamAuthorizationDelegatingHandler>();

builder.Services.AddSingleton<WeatherAgentFactory>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient(nameof(WeatherAgentFactory));
    var kernelBuilder = sp.GetRequiredService<IKernelBuilder>();
    var transport = new HttpClientTransport(new()
    {
        Endpoint = new Uri(applicationSettings.WeatherMcpConfiguration.McpEndpoint),
        OAuth = new()
        {
            RedirectUri = new Uri(applicationSettings.WeatherMcpConfiguration.McpAuthCallbackEndpoint),
            DynamicClientRegistration = new(),
        }
    }, httpClient);

    return new WeatherAgentFactory(kernelBuilder, httpClient, transport);
});

builder.Services.AddScoped<IAuthorizationHeaderProvider, MsalAuthorizationHeaderProvider>();
builder.Services.AddScoped<DownstreamAuthorizationDelegatingHandler>();
builder.Services.AddSingleton<AgentOrchestrationService>();

//CORS
const string CorsPolicyName = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply CORS before authentication/authorization
app.UseCors(CorsPolicyName);

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

static IKernelBuilder CreateKernelBuilder(IServiceCollection services, ApplicationSettings applicationSettings)
{
    var kernelBuilder = Kernel.CreateBuilder();

    kernelBuilder.AddAzureOpenAIChatCompletion(
        deploymentName: applicationSettings.AzureOpenAIConfiguration.DeploymentName,
        endpoint: applicationSettings.AzureOpenAIConfiguration.Endpoint,
        apiKey: applicationSettings.AzureOpenAIConfiguration.ApiKey
    );

    return kernelBuilder;
}
