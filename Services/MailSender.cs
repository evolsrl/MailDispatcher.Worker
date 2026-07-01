using MailDispatcher.Worker.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace MailDispatcher.Worker.Services;

public sealed class MailSender
{
    private readonly ILogger<MailSender> _logger;
    private readonly Office365OAuthTokenProvider _office365OAuthTokenProvider;

    public MailSender(
        ILogger<MailSender> logger,
        Office365OAuthTokenProvider office365OAuthTokenProvider)
    {
        _logger = logger;
        _office365OAuthTokenProvider = office365OAuthTokenProvider;
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

        var tipoAuth = profile.TipoAutenticacion?.Trim().ToUpperInvariant();

        if (tipoAuth == "OAUTH2_OFFICE365")
        {
            if (tracer.ShowSmtpSteps)
                tracer.Write($"SMTP AUTH OAUTH2 -> User={profile.Usuario}");

            var accessToken = await _office365OAuthTokenProvider.GetAccessTokenAsync(profile, ct);

            var oauth2 = new SaslMechanismOAuth2(profile.Usuario, accessToken);
            await client.AuthenticateAsync(oauth2, ct);
        }
        else if (!string.IsNullOrWhiteSpace(profile.Usuario))
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