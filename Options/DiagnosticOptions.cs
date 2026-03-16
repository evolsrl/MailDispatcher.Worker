namespace MailDispatcher.Worker.Options;

public sealed class DiagnosticOptions
{
    public bool Enabled { get; set; }
    public bool ShowTenantStartEnd { get; set; } = true;
    public bool ShowMailStartEnd { get; set; } = true;
    public bool ShowRecipients { get; set; } = true;
    public bool ShowProfileName { get; set; } = true;
    public bool ShowInlineImages { get; set; } = true;
    public bool ShowAttachments { get; set; } = true;
    public bool ShowSmtpSteps { get; set; } = true;
    public bool ShowElapsedMs { get; set; } = true;
}