namespace MailDispatcher.Worker.Models;

public sealed class MailProfileConfig
{
    public int IdParametroMail { get; set; }
    public string Servidor { get; set; } = "";
    public string Usuario { get; set; } = "";
    public string Contrasena { get; set; } = "";
    public bool HabilitarSSL { get; set; }
    public string De { get; set; } = "";
    public string? DeMostrar { get; set; }
    public int Puerto { get; set; }
    public string ProfileName { get; set; } = "";
    public int IdEstado { get; set; }
    public int CantidadDeMailsPorHora { get; set; }
}