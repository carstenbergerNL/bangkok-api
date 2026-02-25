using System.Text.Json;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bangkok.Api.Configuration;

/// <summary>
/// Applies API version and deprecation info from API Explorer to Swagger operations.
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;
        // Deprecation: set operation.Deprecated = true when using [ApiVersion(..., Deprecated = true)] if your package provides IsDeprecated() extension

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            if (!operation.Responses.TryGetValue(responseKey, out var response))
                continue;
            foreach (var contentType in response.Content.Keys.ToList())
            {
                if (!responseType.ApiResponseFormats.Any(x => x.MediaType == contentType))
                    response.Content.Remove(contentType);
            }
        }

        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.FirstOrDefault(p => p.Name == parameter.Name);
            if (description == null)
                continue;
            parameter.Description ??= description.ModelMetadata?.Description;
            if (parameter.Schema.Default == null && description.DefaultValue != null && description.ModelMetadata?.ModelType != null)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(description.DefaultValue, description.ModelMetadata.ModelType);
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
            }
            parameter.Required |= description.IsRequired;
        }
    }
}
