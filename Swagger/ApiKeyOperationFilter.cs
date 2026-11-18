using BiblioLink.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BiblioLink.Swagger;

public class ApiKeyOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var metadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;
        var requirement = new OpenApiSecurityRequirement();
        if (metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any())
        {
            requirement[new OpenApiSecuritySchemeReference("Bearer", context.Document, null)] = new List<string>();
        }

        if (metadata.OfType<ApiKeyAttribute>().Any())
        {
            requirement[new OpenApiSecuritySchemeReference("ApiKey", context.Document, null)] = new List<string>();
        }

        if (requirement.Count > 0)
        {
            operation.Security ??= new List<OpenApiSecurityRequirement>();
            operation.Security.Add(requirement);
        }
    }
}
