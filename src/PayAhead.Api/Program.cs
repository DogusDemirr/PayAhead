using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using PayAhead.Api;
using PayAhead.Application;
using PayAhead.Infrastructure;
using Serilog;
using PayAhead.Domain;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, logger) => logger.ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext().WriteTo.Console());
_ = DevelopmentIdentity.Validate(builder.Environment, builder.Configuration);
builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();
builder.Services.AddHostedService<NotificationWorker>();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<PaymentFrequency>(allowIntegerValues: false));
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter<OccurrenceStatus>(allowIntegerValues: false));
});
builder.Services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
{
    // JSON parse mesajları istemcinin hassas alan değerlerini tekrar etmesin.
    var errors = context.ModelState.Where(x => x.Value?.Errors.Count > 0)
        .ToDictionary(x => x.Key, _ => new[] { "Alan biçimi veya değeri geçersiz." });
    var problem = new ValidationProblemDetails(errors) { Status = 400, Title = "İstek doğrulanamadı." };
    problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    var result = new BadRequestObjectResult(problem);
    result.ContentTypes.Add("application/problem+json");
    return result;
});
builder.Services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("sqlserver", tags: ["ready"]);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "PayAhead API", Version = "v1", Description = "Faz 7: JWT, ödeme/occurrence/overview, reminder, device ve settings API. Bildirimler SQL tabanlı worker üzerinden işlenir; push token yanıtlar içinde gösterilmez." });
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme { Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" });
    options.DocumentFilter<HealthDocumentFilter>();
    options.SchemaFilter<PaymentSchemaFilter>();
    options.SchemaFilter<OccurrenceSchemaFilter>();
    options.OperationFilter<AuthenticationOperationFilter>();
});

builder.Services.AddPayAheadAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

var app = builder.Build();
app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages(async context => await Results.Problem(statusCode: context.HttpContext.Response.StatusCode)
    .ExecuteAsync(context.HttpContext));
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseSerilogRequestLogging(options => options.MessageTemplate = "HTTP {RequestMethod} {StatusCode} {Elapsed:0.0000} ms");
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "Healthy" })).AllowAnonymous()
    .WithName("Liveness").WithTags("Health").Produces(200);
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        if (report.Status == Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy)
            await context.Response.WriteAsJsonAsync(new { status = "Healthy" }, context.RequestAborted);
        else await Results.Problem(statusCode: 503, title: "Hizmet hazır değil.").ExecuteAsync(context);
    }
}).AllowAnonymous();
app.Run();

public partial class Program;
