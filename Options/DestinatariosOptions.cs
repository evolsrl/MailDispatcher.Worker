namespace MailDispatcher.Worker.Options;

/// <summary>
/// Sección "Destinatarios" de appsettings. En desarrollo y pruebas las bases tienen correos reales de socios:
/// con <see cref="SoloPermitidos"/> el Worker solo envía a las direcciones o dominios de <see cref="Permitidos"/>
/// y quita al resto. Si a un mail no le queda ningún destinatario, no se envía y queda en
/// <see cref="IdEstadoNoEnviado"/>. Fuera de Production el filtro está siempre activo (ver Program.cs).
/// </summary>
public sealed class DestinatariosOptions
{
    public bool SoloPermitidos { get; set; }

    /// <summary>Direcciones exactas ("persona@dominio.com") o dominios completos ("@evol.com.ar").</summary>
    public List<string> Permitidos { get; set; } = [];

    /// <summary>Estado en que queda un mail sin destinatarios permitidos (151 = "No Enviado" en TGEEstados).</summary>
    public int IdEstadoNoEnviado { get; set; } = 151;

    /// <summary>Texto que se antepone al asunto cuando el filtro está activo, por ejemplo "[PRUEBAS] ".</summary>
    public string? PrefijoAsunto { get; set; }
}
