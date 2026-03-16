using MimeKit;

namespace MailDispatcher.Worker.Models;

public sealed class ComposeResult
{
    public MimeMessage Message { get; set; } = default!;
    public List<string> InlineFound { get; set; } = [];
    public List<string> InlineMissing { get; set; } = [];
    public List<string> AttachmentsFound { get; set; } = [];
    public List<string> AttachmentsMissing { get; set; } = [];
    public List<string> CidsDetected { get; set; } = [];
}