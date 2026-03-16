using MailDispatcher.Worker.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailDispatcher.Worker.Services;

public sealed class MailSender
{
    private readonly ILogger<MailSender> _logger;

    public MailSender(ILogger<MailSender> logger)
    {
        _logger = logger;
    }

    public async Task SendAsync(
        MimeMessage message,
        MailProfileConfig profile,
        DiagnosticTracer tracer,
        CancellationToken ct)
    {
        using var client = new SmtpClient();

        var secureSocketOptions = profile.HabilitarSSL
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.Auto;

        _logger.LogInformation("SMTP connect Host={Host} Port={Port} SSL={Ssl}", profile.Servidor, profile.Puerto, profile.HabilitarSSL);

        if (tracer.ShowSmtpSteps)
            tracer.Write($"SMTP CONNECT -> Host={profile.Servidor} Port={profile.Puerto} SSL={profile.HabilitarSSL}");

        await client.ConnectAsync(profile.Servidor, profile.Puerto, secureSocketOptions, ct);

        if (!string.IsNullOrWhiteSpace(profile.Usuario))
        {
            _logger.LogInformation("SMTP auth User={User}", profile.Usuario);

            if (tracer.ShowSmtpSteps)
                tracer.Write($"SMTP AUTH -> User={profile.Usuario}");

            await client.AuthenticateAsync(profile.Usuario, profile.Contrasena, ct);
        }

        _logger.LogInformation("SMTP send Subject={Subject}", message.Subject);

        if (tracer.ShowSmtpSteps)
            tracer.Write("SMTP SEND -> Enviando mensaje");

        await client.SendAsync(message, ct);

        if (tracer.ShowSmtpSteps)
            tracer.Write("SMTP DISCONNECT");

        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("SMTP send OK Subject={Subject}", message.Subject);
    }
}