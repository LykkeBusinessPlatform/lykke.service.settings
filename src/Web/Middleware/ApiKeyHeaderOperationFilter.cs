using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Web.Middleware
{
    public class ApiKeyHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (!context.ApiDescription.RelativePath.Contains("api/"))
                return;

            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Authorization",
                In = ParameterLocation.Header,
                Description = "API key",
                Required = true,
                Schema = new OpenApiSchema
                {
                    Type = "String",
                    Default = new OpenApiString("API key")
                }
            });
        }
    }
}
