using System.Text.Json;
using MailDispatcher.Worker.Models;
using Microsoft.Identity.Client;

namespace MailDispatcher.Worker.Services;

public sealed class Office365OAuthTokenProvider
{
    public async Task<string> GetAccessTokenAsync(MailProfileConfig profile, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(profile.OAuthConfiguracion))
            throw new InvalidOperationException("OAuthConfiguracion está vacío.");

        var cfg = JsonSerializer.Deserialize<Office365OAuthConfig>(
            profile.OAuthConfiguracion,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (cfg is null)
            throw new InvalidOperationException("OAuthConfiguracion no tiene formato válido.");

        var app = ConfidentialClientApplicationBuilder
            .Create(cfg.ClientId)
            .WithClientSecret(cfg.ClientSecret)
            .WithTenantId(cfg.TenantId)
            .Build();

        var result = await app
            .AcquireTokenForClient(new[] { cfg.Scope })
            .ExecuteAsync(ct);

        return result.AccessToken;
    }
}