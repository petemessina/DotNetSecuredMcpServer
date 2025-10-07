namespace McpServerAuthDemo.Models;

public record ApplicationSettings(
    string[] JwtIssuers,
    string JwtAudience,
    string JwtAuthority,
    string[] SupportedClaims,
    string FrontendCorsOrigin,
    WeatherApiSettings WeatherApiSettings
);

public record WeatherApiSettings(
    string BaseUrl,
    string ProductName,
    string ProductVersion
);
