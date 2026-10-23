using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PayAhead.Application.Auth;

namespace PayAhead.Infrastructure;

public sealed class SmtpPasswordResetEmail(IConfiguration config, ILogger<SmtpPasswordResetEmail> logger) : IPasswordResetEmail
{
    public async Task<bool> SendAsync(string email, string link, int lifetimeMinutes, CancellationToken ct)
    {
        if (!config.GetValue<bool>("Email:Enabled")) { logger.LogWarning("Password reset email transport is disabled."); return false; }
        try
        {
            using var smtp = new SmtpClient(config["Email:Host"], config.GetValue("Email:Port", 587))
            { EnableSsl = config.GetValue("Email:EnableSsl", true), UseDefaultCredentials = false, Timeout = 10000 };
            if (!string.IsNullOrEmpty(config["Email:Username"])) smtp.Credentials = new NetworkCredential(config["Email:Username"], config["Email:Password"]);
            using var message = new MailMessage(config["Email:From"]!, email) { Subject = "PayAhead · Şifreni yenile", IsBodyHtml = true };
            var safe = WebUtility.HtmlEncode(link);
            message.Body = $"""
                <!doctype html><html lang="tr"><meta name="viewport" content="width=device-width, initial-scale=1"><body style="margin:0;background:#f5f7fa;font-family:Arial,sans-serif;color:#101d35"><main style="max-width:480px;margin:24px auto;padding:28px;background:white;border-radius:20px"><h1>PayAhead</h1><p style="color:#087c70">Yarına bir adım önde.</p><h2>Şifreni yenile</h2><p>Yeni şifreni belirlemek için aşağıdaki bağlantıyı aç. Bu tek kullanımlık bağlantı {lifetimeMinutes} dakika geçerlidir.</p><p><a href="{safe}" style="display:inline-block;padding:16px;background:#087c70;color:white;border-radius:14px">Yeni şifre belirle</a></p><p>Expo Go'da uygulamanın açık ve geliştirme sunucusunun erişilebilir olması gerekir. Bağlantı açılmazsa uygulamadaki “Yeni şifre belirle” ekranına e-postadaki bağlantının tamamını yapıştır.</p><p style="overflow-wrap:anywhere">{safe}</p><p>Bu isteği sen yapmadıysan bu e-postayı yok say. Şifren değişmedi.</p></main></body></html>
                """;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct); deadline.CancelAfter(TimeSpan.FromSeconds(10));
            await smtp.SendMailAsync(message, deadline.Token); return true;
        }
        catch (Exception ex) when (ex is SmtpException or OperationCanceledException or InvalidOperationException or FormatException or ArgumentException)
        { logger.LogWarning("Password reset email transport failed; no recipient or secret is logged."); return false; }
    }
}
