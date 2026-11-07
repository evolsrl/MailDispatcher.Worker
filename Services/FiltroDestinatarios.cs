using MailDispatcher.Worker.Options;
using MimeKit;

namespace MailDispatcher.Worker.Services;

public sealed class FiltroDestinatariosResultado
{
    public List<string> Permitidos { get; } = [];
    public List<string> Quitados { get; } = [];
    public bool HayDestinatarios => Permitidos.Count > 0;
}

/// <summary>
/// Deja en el mensaje (To, Cc y Bcc) solo los destinatarios permitidos por <see cref="DestinatariosOptions"/>.
/// Con la lista de permitidos vacía no queda ninguno: en desarrollo y pruebas no se envía nada.
/// </summary>
public static class FiltroDestinatarios
{
    public static FiltroDestinatariosResultado Aplicar(MimeMessage message, DestinatariosOptions options)
    {
        var resultado = new FiltroDestinatariosResultado();

        Filtrar(message.To, options.Permitidos, resultado);
        Filtrar(message.Cc, options.Permitidos, resultado);
        Filtrar(message.Bcc, options.Permitidos, resultado);

        var asunto = message.Subject ?? string.Empty;
        if (resultado.HayDestinatarios && !string.IsNullOrEmpty(options.PrefijoAsunto)
            && !asunto.StartsWith(options.PrefijoAsunto, StringComparison.Ordinal))
            message.Subject = options.PrefijoAsunto + asunto;

        return resultado;
    }

    /// <summary>
    /// Revisa las direcciones tal como vienen de AudMailsEnvios (A, Copia, CopiaOculta separadas por ';'), antes de
    /// armar el mensaje: así un mail sin destinatarios permitidos no llega a pedir la configuración SMTP.
    /// </summary>
    public static bool HayAlgunPermitido(IEnumerable<string> permitidos, params string?[] listas)
    {
        var lista = permitidos.ToList();
        foreach (var raw in listas)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            foreach (var parte in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var direccion = MailboxAddress.TryParse(parte, out var mailbox) ? mailbox.Address : parte;
                if (EsPermitido(direccion, lista))
                    return true;
            }
        }
        return false;
    }

    public static bool EsPermitido(string direccion, IEnumerable<string> permitidos)
    {
        var dir = direccion.Trim();
        foreach (var p in permitidos)
        {
            var permitido = p?.Trim();
            if (string.IsNullOrEmpty(permitido))
                continue;

            // "@evol.com.ar" habilita todo el dominio (y no "x@evol.com.ar.otro.com").
            if (permitido.StartsWith('@'))
            {
                if (dir.EndsWith(permitido, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            else if (string.Equals(dir, permitido, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static void Filtrar(InternetAddressList lista, List<string> permitidos, FiltroDestinatariosResultado resultado)
    {
        foreach (var direccion in lista.ToList())
        {
            // Los grupos de direcciones no se revisan uno por uno: con el filtro activo no se envían.
            if (direccion is MailboxAddress mailbox && EsPermitido(mailbox.Address, permitidos))
            {
                resultado.Permitidos.Add(mailbox.Address);
                continue;
            }

            resultado.Quitados.Add(direccion is MailboxAddress m ? m.Address : direccion.ToString());
            lista.Remove(direccion);
        }
    }
}
