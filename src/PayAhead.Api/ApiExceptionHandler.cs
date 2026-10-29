using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PayAhead.Application.Payments;
using PayAhead.Domain;
using PayAhead.Application.Premium;

namespace PayAhead.Api;

public sealed class ApiExceptionHandler(IProblemDetailsService problems, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var status = exception switch
        {
            PayAhead.Application.Auth.AvatarException avatar => avatar.Code switch { "avatar_too_large" => 413, "avatar_changed" => 409, "avatar_unavailable" => 503, _ => 400 },
            ValidationException or DomainRuleException or PayAhead.Application.Auth.CurrentPasswordException => 400,
            BadHttpRequestException { StatusCode: 413 } => 413,
            BadHttpRequestException => 400,
            StoreVerificationException => 400,
            EntitlementLimitException => 403,
            StoreUnavailableException => 503,
            PayAhead.Application.Auth.PasswordResetUnavailableException => 503,
            PayAhead.Application.Operations.OperationalUnavailableException => 503,
            PayAhead.Application.Operations.ExportLimitException => 413,
            AuthenticationRequiredException => 401,
            ResourceNotFoundException => 404,
            DbUpdateConcurrencyException or ResourceConflictException or PaymentDeletionRestrictedException => 409,
            _ => 500
        };
        // İstisna mesajı/veritabanı parametreleri veya istek gövdesi loglanmaz.
        if (status == 500) logger.LogError("İstek başarısız. ExceptionType={ExceptionType} TraceId={TraceId}",
            exception.GetType().Name, context.TraceIdentifier);
        if (context.Request.Path.StartsWithSegments("/api/users/me/avatar"))
            logger.LogWarning("Avatar request rejected. StatusCode={StatusCode} Code={Code} ExceptionType={ExceptionType} TraceId={TraceId}",
                status, exception is PayAhead.Application.Auth.AvatarException avatarFailure ? avatarFailure.Code : "avatar_failed",
                exception.GetType().Name, context.TraceIdentifier);
        if (exception is PayAhead.Application.Auth.PasswordResetUnavailableException)
            logger.LogWarning("Password reset schema is unavailable. Pending migrations require operator review.");
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
            403 => "Üyelik planının aktif kayıt sınırı aşıldı.",
            503 => exception is PayAhead.Application.Auth.AvatarException ? "Profil fotoğrafı hizmeti şu anda kullanılamıyor." : exception is PayAhead.Application.Auth.PasswordResetUnavailableException ? "Şifre yenileme hizmeti şu anda kullanılamıyor." : "Mağaza doğrulaması şu anda kullanılamıyor.",
            413 => exception is PayAhead.Application.Operations.ExportLimitException ? "Veri dışa aktarım sınırı aşıldı; destek üzerinden parçalara ayrılmış aktarım isteyin." : "İstek gövdesi çok büyük.",
            404 => context.Request.Path.StartsWithSegments("/api/users/me/avatar") ? "Profil fotoğrafı bulunamadı." : "Ödeme planı bulunamadı.",
            409 => exception is PaymentDeletionRestrictedException ? "Bağlı kayıtları olan plan silinemiyor." : exception is ResourceConflictException ? exception.Message : "Kaynak başka bir işlem tarafından değiştirildi.",
            _ => "Beklenmeyen sunucu hatası."
        };
        detail.Extensions["traceId"] = context.TraceIdentifier;
        if (exception is EntitlementLimitException) detail.Extensions["code"] = "entitlement_limit";
        if (exception is PayAhead.Application.Auth.CurrentPasswordException) detail.Extensions["code"] = "current_password_invalid";
        if (exception is PayAhead.Application.Auth.AvatarException avatarError) detail.Extensions["code"] = avatarError.Code;
        if (exception is PaymentDeletionRestrictedException restricted) detail.Extensions["code"] = restricted.Code;
        else if (HttpMethods.IsDelete(context.Request.Method) && context.Request.Path.StartsWithSegments("/api/payments") && exception is DbUpdateConcurrencyException or ResourceConflictException) detail.Extensions["code"] = "payment_changed";
        if (exception is DomainRuleException) detail.Detail = exception.Message;
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = detail
        });
    }
}
