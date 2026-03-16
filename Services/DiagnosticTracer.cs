using MailDispatcher.Worker.Options;
using Microsoft.Extensions.Options;

namespace MailDispatcher.Worker.Services;

public sealed class DiagnosticTracer
{
    private readonly DiagnosticOptions _options;
    private readonly IHostEnvironment _environment;

    public DiagnosticTracer(IOptions<DiagnosticOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _environment = environment;
    }

    public bool Enabled => _options.Enabled || _environment.IsDevelopment();

    public bool ShowTenantStartEnd => Enabled && _options.ShowTenantStartEnd;
    public bool ShowMailStartEnd => Enabled && _options.ShowMailStartEnd;
    public bool ShowRecipients => Enabled && _options.ShowRecipients;
    public bool ShowProfileName => Enabled && _options.ShowProfileName;
    public bool ShowInlineImages => Enabled && _options.ShowInlineImages;
    public bool ShowAttachments => Enabled && _options.ShowAttachments;
    public bool ShowSmtpSteps => Enabled && _options.ShowSmtpSteps;
    public bool ShowElapsedMs => Enabled && _options.ShowElapsedMs;

    public void Write(string message)
    {
        if (!Enabled) return;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
    }
}