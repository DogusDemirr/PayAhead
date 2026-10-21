using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace PayAhead.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var conflict = exception is DbUpdateConcurrencyException;
        // İstisna mesajı/veritabanı parametreleri veya istek gövdesi loglanmaz.
        logger.LogError("İstek başarısız. ExceptionType={ExceptionType} TraceId={TraceId}",
            exception.GetType().Name, context.TraceIdentifier);
        context.Response.StatusCode = conflict ? 409 : 500;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new()
            {
                Status = context.Response.StatusCode,
                Title = conflict ? "Kaynak başka bir işlem tarafından değiştirildi." : "Beklenmeyen sunucu hatası.",
                Extensions = { ["traceId"] = context.TraceIdentifier }
            }
        });
    }
}
