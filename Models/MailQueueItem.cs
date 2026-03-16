namespace MailDispatcher.Worker.Models;

public sealed class MailQueueItem
{
    public int IdMailEnvio { get; set; }
    public string ProfileName { get; set; } = "";
    public string? De { get; set; }
    public string A { get; set; } = "";
    public string? Copia { get; set; }
    public string? CopiaOculta { get; set; }
    public string Cuerpo { get; set; } = "";
    public DateTime? FechaEnvio { get; set; }
    public string Asunto { get; set; } = "";
    public string? Adjuntos { get; set; }
}