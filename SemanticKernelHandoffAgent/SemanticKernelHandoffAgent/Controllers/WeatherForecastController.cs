using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SemanticKernelHandoffAgent.Models;
using SemanticKernelHandoffAgent.Services;

namespace SemanticKernelHandoffAgent.Controllers;
    
[Authorize]
[ApiController]
[Route("[controller]")]
public sealed class WeatherForecastController(
    AgentOrchestrationService agentOrchestrationService,
    ILogger<WeatherForecastController> logger
) : ControllerBase {

    [HttpPost(Name = "GetWeatherForecast")]
    public async Task<WeatherResponse> Get(WeatherRequest weatherRequest)
    {
        logger.LogInformation("Received weather request: {Message}", weatherRequest.Message);
        var response = await agentOrchestrationService.InvokeOrchestrationAsync(weatherRequest.Message);
        logger.LogInformation("Weather response: {Response}", response);

        return new WeatherResponse(response);
    }
}
