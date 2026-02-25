using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bangkok.Api.Configuration;

/// <summary>
/// Configures Swagger document per API version (from API versioning explorer).
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider) => _provider = provider;

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Bangkok API",
                    Description = "Enterprise Web API foundation with JWT authentication. Version " + description.ApiVersion,
                    Version = description.ApiVersion.ToString()
                });
        }
    }
}
