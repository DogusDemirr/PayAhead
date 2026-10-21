using Microsoft.OpenApi.Models;
using PayAhead.Application.Occurrences;
using PayAhead.Domain;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace PayAhead.Api;

public sealed class OccurrenceSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type != typeof(SetOccurrenceStatusRequest)) return;
        schema.Required.Add("status"); schema.Properties["status"].Nullable = false;
        schema.Properties["actualAmount"].Minimum = 0;
        schema.Properties["actualAmount"].ExclusiveMinimum = true;
        schema.Properties["actualAmount"].Maximum = PaymentPlanRules.MaximumAmount;
        schema.Properties["actualAmount"].MultipleOf = 0.0001m;
        schema.Properties["actualAmount"].Description = "Paid için zorunlu; Pending ve Skipped için null. Plan tutarı varsayılmaz.";
        schema.Properties["rowVersion"].MinLength = 12; schema.Properties["rowVersion"].MaxLength = 12;
        schema.Properties["rowVersion"].Description = "Mevcut kaydı farklı bir duruma/tutara geçirmek için son occurrence rowVersion değeri. Aynı durum/tutar tekrarı yazma yapmaz.";
    }
}
