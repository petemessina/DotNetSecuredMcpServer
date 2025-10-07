using McpServerAuthDemo.Models;
using McpServerAuthDemo.Tools;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);
var applicationSettings = builder.Configuration
    .GetSection("ApplicationSettings")
    .Get<ApplicationSettings>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    // Configure to validate tokens from our in-memory OAuth server
    options.Authority = applicationSettings.JwtAuthority;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidAudience = applicationSettings.JwtAudience,
        ValidIssuers = applicationSettings.JwtIssuers
    };
})
.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {
        Resource = new Uri(applicationSettings.JwtAudience),
        AuthorizationServers = applicationSettings.JwtIssuers.Select(issuer => new Uri(issuer)).ToList(),
        ScopesSupported = applicationSettings.SupportedClaims.ToList(),
    };
});

//CORS
const string CorsPolicyName = "FrontendDev";
builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy.WithOrigins(applicationSettings.FrontendCorsOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMcpServer()
    .WithTools<WeatherTools>()
    .WithHttpTransport();

// Configure HttpClientFactory for weather.gov API
builder.Services.AddHttpClient("WeatherApi", client =>
{
    client.BaseAddress = new Uri(applicationSettings.WeatherApiSettings.BaseUrl);
    client.DefaultRequestHeaders.UserAgent.Add(
        new ProductInfoHeaderValue(
            applicationSettings.WeatherApiSettings.ProductName,
            applicationSettings.WeatherApiSettings.ProductVersion
        )
    );
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply CORS before authentication/authorization
app.UseCors(CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp")
    .RequireAuthorization();

app.Run();
