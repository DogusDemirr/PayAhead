using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PayAhead.Api;

public sealed class HealthDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        document.Paths["/health/ready"] = new OpenApiPathItem
        {
            Operations =
            {
                [OperationType.Get] = new OpenApiOperation
                {
                    OperationId = "Readiness",
                    Summary = "SQL Server bağlantısı ve migration hazırlığı",
                    Tags = [new OpenApiTag { Name = "Health" }],
                    Responses = new OpenApiResponses
                    {
                        ["200"] = new OpenApiResponse
                        {
                            Description = "Hazır",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/json"] = new() { Schema = new OpenApiSchema
                                {
                                    Type = "object", Properties = new Dictionary<string, OpenApiSchema>
                                    { ["status"] = new() { Type = "string" } }
                                } }
                            }
                        },
                        ["503"] = new OpenApiResponse
                        {
                            Description = "Hazır değil",
                            Content = new Dictionary<string, OpenApiMediaType>
                            {
                                ["application/problem+json"] = new()
                                {
                                    Schema = context.SchemaGenerator.GenerateSchema(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), context.SchemaRepository)
                                }
                            }
                        }
                    }
                }
            }
        };
    }
}
