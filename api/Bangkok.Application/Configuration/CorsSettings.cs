namespace Bangkok.Application.Configuration;

public class CorsSettings
{
    public const string SectionName = "Cors";

    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
    public string[] AllowedHeaders { get; set; } = new[] { "Content-Type", "Authorization", "X-Correlation-ID" };
    public string[] AllowedMethods { get; set; } = new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" };
}
