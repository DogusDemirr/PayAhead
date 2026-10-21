using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Payments;
using PayAhead.Domain;

namespace PayAhead.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            ValidationException or DomainRuleException => 400,
            AuthenticationRequiredException => 401,
            ResourceNotFoundException => 404,
            DbUpdateConcurrencyException or ResourceConflictException => 409,
            _ => 500
        };
        // İstisna mesajı/veritabanı parametreleri veya istek gövdesi loglanmaz.
        if (status == 500) logger.LogError("İstek başarısız. ExceptionType={ExceptionType} TraceId={TraceId}",
            exception.GetType().Name, context.TraceIdentifier);
        ProblemDetails detail = exception is ValidationException validation
            ? new ValidationProblemDetails(validation.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(g => System.Text.Json.JsonNamingPolicy.CamelCase.ConvertName(g.Key),
                    g => g.Select(x => x.ErrorMessage).Distinct().ToArray()))
            : new ProblemDetails();
        detail.Status = status;
        detail.Title = status switch
        {
            400 => "İstek doğrulanamadı.",
            401 => "Kimlik doğrulama gerekli.",
            404 => "Ödeme planı bulunamadı.",
            409 => exception is ResourceConflictException ? exception.Message : "Kaynak başka bir işlem tarafından değiştirildi.",
            _ => "Beklenmeyen sunucu hatası."
        };
        detail.Extensions["traceId"] = context.TraceIdentifier;
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = detail
        });
    }
}
