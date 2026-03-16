namespace MailDispatcher.Worker.Models;

public sealed class TenantConfig
{
    public string Grupo { get; set; } = "";
    public string Empresa { get; set; } = "";
    public string BaseDatos { get; set; } = "";
    public string? URLSistema { get; set; }
    public int IdEstado { get; set; }

    public bool MailHabilitado { get; set; }
    public int MailOrdenProceso { get; set; }
    public string? MailInlineImagesPath { get; set; }
    public string? MailSqlInstance { get; set; }
}