using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Upm.Application.TienDo;

namespace Upm.Infrastructure.TienDo;

/// <summary>Gửi email qua SMTP (dev: Mailpit <c>localhost:1025</c>). Lỗi gửi được ghi log, không làm hỏng job.</summary>
public sealed class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task GuiAsync(string denEmail, string tieuDe, string noiDung, CancellationToken ct = default)
    {
        var host = configuration["Smtp:Host"] ?? "localhost";
        var port = configuration.GetValue("Smtp:Port", 1025);
        var from = configuration["Smtp:From"] ?? "upm@utc.edu.vn";
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(denEmail));
        message.Subject = tieuDe;
        message.Body = new TextPart("plain") { Text = noiDung };
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.None, ct);
            await client.SendAsync(message, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Không gửi được email tới {Email}.", denEmail);
        }
    }
}
