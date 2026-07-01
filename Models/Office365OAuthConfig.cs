namespace MailDispatcher.Worker.Models;

public sealed class Office365OAuthConfig
{
    public string TenantId { get; set; } = "";
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
    public string Scope { get; set; } = "https://outlook.office365.com/.default";
}