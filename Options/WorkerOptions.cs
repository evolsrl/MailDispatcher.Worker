namespace MailDispatcher.Worker.Options;

public sealed class WorkerOptions
{
    public int PollSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 15;
    public string InlineImagesPath { get; set; } = @"C:\MailInline";
    public bool RunOnce { get; set; }
    public int? IdMailEnvio { get; set; }
}