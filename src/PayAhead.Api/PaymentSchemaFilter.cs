using Microsoft.OpenApi.Models;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PayAhead.Api;

// FluentValidation çalışma zamanı kurallarının OpenAPI'deki karşılığı.
public sealed class PaymentSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (!typeof(PaymentWriteRequest).IsAssignableFrom(context.Type)) return;
        foreach (var name in new[] { "name", "amount", "currencyCode", "category", "frequency", "interval", "startDate" })
        {
            schema.Required.Add(name);
            schema.Properties[name].Nullable = false;
        }
        schema.Properties["name"].MinLength = 1;
        schema.Properties["name"].MaxLength = 150;
        schema.Properties["description"].MaxLength = 2000;
        schema.Properties["category"].MaxLength = 50;
        schema.Properties["amount"].Minimum = 0;
        schema.Properties["amount"].ExclusiveMinimum = true;
        schema.Properties["amount"].Maximum = PaymentPlanRules.MaximumAmount;
        schema.Properties["amount"].MultipleOf = 0.0001m;
        schema.Properties["currencyCode"].Description = "SIX ISO 4217 güncel listesindeki kod; trim ve invariant uppercase ile saklanır.";
        schema.Properties["interval"].Minimum = 1;
        schema.Properties["anchorDay"].Minimum = 1;
        schema.Properties["anchorDay"].Maximum = 31;
        if (context.Type == typeof(UpdatePaymentRequest))
        {
            schema.Required.Add("rowVersion");
            var version = schema.Properties["rowVersion"];
            version.Nullable = false;
            version.MinLength = 12; version.MaxLength = 12;
            version.Description = "Son GET/POST/PUT yanıtındaki 8 baytlık SQL rowversion, canonical Base64. Eski sürüm 409 üretir.";
        }
    }
}
