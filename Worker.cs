using System.Diagnostics;
using MailDispatcher.Worker.Data;
using MailDispatcher.Worker.Options;
using MailDispatcher.Worker.Services;
using Microsoft.Extensions.Options;

namespace MailDispatcher.Worker;

public sealed class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly TenantRepository _tenantRepository;
    private readonly MailRepository _mailRepository;
    private readonly MailComposer _composer;
    private readonly MailSender _sender;
    private readonly TenantConnectionStringFactory _connectionFactory;
    private readonly WorkerOptions _options;
    private readonly DiagnosticTracer _tracer;

    public Worker(
        ILogger<Worker> logger,
        TenantRepository tenantRepository,
        MailRepository mailRepository,
        MailComposer composer,
        MailSender sender,
        TenantConnectionStringFactory connectionFactory,
        IOptions<WorkerOptions> options,
        DiagnosticTracer tracer)
    {
        _logger = logger;
        _tenantRepository = tenantRepository;
        _mailRepository = mailRepository;
        _composer = composer;
        _sender = sender;
        _connectionFactory = connectionFactory;
        _options = options.Value;
        _tracer = tracer;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        do
        {
            await ProcesarCicloAsync(stoppingToken);

            if (_options.RunOnce)
                break;

            await Task.Delay(TimeSpan.FromSeconds(_options.PollSeconds), stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);
    }

    private async Task ProcesarCicloAsync(CancellationToken ct)
    {
        var tenants = await _tenantRepository.ObtenerTenantsActivosAsync(ct);

        foreach (var tenant in tenants)
        {
            var swTenant = Stopwatch.StartNew();

            try
            {
                if (_tracer.ShowTenantStartEnd)
                    _tracer.Write($"TENANT START -> Base={tenant.BaseDatos} Empresa={tenant.Empresa}");

                var tenantConnection = _connectionFactory.Build(tenant.BaseDatos, tenant.MailSqlInstance);
                var inlinePath = tenant.MailInlineImagesPath ?? string.Empty;

                var mails = await _mailRepository.TomarPendientesAsync(
                    tenantConnection,
                    _options.IdMailEnvio,
                    _options.BatchSize,
                    ct);

                if (mails.Count == 0)
                {
                    if (_tracer.ShowTenantStartEnd)
                        _tracer.Write($"TENANT EMPTY -> Base={tenant.BaseDatos}");
                    continue;
                }

                foreach (var item in mails)
                {
                    var swMail = Stopwatch.StartNew();

                    try
                    {
                        if (_tracer.ShowMailStartEnd)
                            _tracer.Write($"MAIL START -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio}");

                        if (_tracer.ShowRecipients)
                            _tracer.Write($"RECIPIENTS -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} To={item.A} Cc={item.Copia} Bcc={item.CopiaOculta}");

                        await _mailRepository.LogProcesoAsync(
                            tenantConnection,
                            item.IdMailEnvio,
                            "WORKER",
                            "INFO",
                            $"Inicio de procesamiento tenant={tenant.BaseDatos}",
                            null,
                            ct);

                        var cfg = await _mailRepository.ObtenerConfigAsync(tenantConnection, item.ProfileName, ct);
                        if (cfg is null)
                            throw new InvalidOperationException($"No se encontró configuración SMTP para ProfileName={item.ProfileName}");

                        if (_tracer.ShowProfileName)
                            _tracer.Write($"PROFILE -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} ProfileName={item.ProfileName}");

                        var composeResult = _composer.Compose(item, cfg, inlinePath);

                        await _mailRepository.LogProcesoAsync(
                            tenantConnection,
                            item.IdMailEnvio,
                            "WORKER",
                            "INFO",
                            "Resumen de recursos del mail",
                            $"CIDs={composeResult.CidsDetected.Count}; InlineFound={composeResult.InlineFound.Count}; InlineMissing={composeResult.InlineMissing.Count}; AttachFound={composeResult.AttachmentsFound.Count}; AttachMissing={composeResult.AttachmentsMissing.Count}",
                            ct);

                        if (_tracer.ShowInlineImages)
                        {
                            _tracer.Write($"INLINE PATH -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} Path={inlinePath}");

                            if (composeResult.CidsDetected.Count > 0)
                                _tracer.Write($"CID DETECTED -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} [{string.Join(", ", composeResult.CidsDetected)}]");

                            if (composeResult.InlineFound.Count > 0)
                                _tracer.Write($"INLINE FOUND -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} [{string.Join(" | ", composeResult.InlineFound)}]");

                            if (composeResult.InlineMissing.Count > 0)
                                _tracer.Write($"INLINE MISSING -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} [{string.Join(" | ", composeResult.InlineMissing)}]");
                        }

                        if (_tracer.ShowAttachments)
                        {
                            if (composeResult.AttachmentsFound.Count > 0)
                                _tracer.Write($"ATTACH FOUND -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} [{string.Join(" | ", composeResult.AttachmentsFound)}]");

                            if (composeResult.AttachmentsMissing.Count > 0)
                                _tracer.Write($"ATTACH MISSING -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} [{string.Join(" | ", composeResult.AttachmentsMissing)}]");
                        }

                        // LOG SQL: CIDs detectados
                        if (composeResult.CidsDetected.Count > 0)
                        {
                            await _mailRepository.LogProcesoAsync(
                                tenantConnection,
                                item.IdMailEnvio,
                                "WORKER",
                                "INFO",
                                "CID detectados",
                                string.Join(" | ", composeResult.CidsDetected),
                                ct);
                        }

                        // LOG SQL: imágenes inline encontradas
                        if (composeResult.InlineFound.Count > 0)
                        {
                            await _mailRepository.LogProcesoAsync(
                                tenantConnection,
                                item.IdMailEnvio,
                                "WORKER",
                                "INFO",
                                "Imagenes inline encontradas",
                                string.Join(" | ", composeResult.InlineFound),
                                ct);
                        }

                        // LOG SQL: imágenes inline faltantes
                        if (composeResult.InlineMissing.Count > 0)
                        {
                            await _mailRepository.LogProcesoAsync(
                                tenantConnection,
                                item.IdMailEnvio,
                                "WORKER",
                                "WARN",
                                "Imagenes inline faltantes",
                                string.Join(" | ", composeResult.InlineMissing),
                                ct);
                        }

                        // LOG SQL: adjuntos encontrados
                        if (composeResult.AttachmentsFound.Count > 0)
                        {
                            await _mailRepository.LogProcesoAsync(
                                tenantConnection,
                                item.IdMailEnvio,
                                "WORKER",
                                "INFO",
                                "Adjuntos encontrados",
                                string.Join(" | ", composeResult.AttachmentsFound),
                                ct);
                        }

                        // LOG SQL: adjuntos faltantes
                        if (composeResult.AttachmentsMissing.Count > 0)
                        {
                            await _mailRepository.LogProcesoAsync(
                                tenantConnection,
                                item.IdMailEnvio,
                                "WORKER",
                                "WARN",
                                "Adjuntos faltantes",
                                string.Join(" | ", composeResult.AttachmentsMissing),
                                ct);
                        }

                        await _sender.SendAsync(composeResult.Message, cfg, _tracer, ct);

                        await _mailRepository.ActualizarEstadoAsync(
                            tenantConnection,
                            item.IdMailEnvio,
                            107,
                            "Correo enviado correctamente",
                            ct);

                        await _mailRepository.LogProcesoAsync(
                            tenantConnection,
                            item.IdMailEnvio,
                            "WORKER",
                            "INFO",
                            $"Correo enviado correctamente tenant={tenant.BaseDatos}",
                            null,
                            ct);

                        swMail.Stop();

                        if (_tracer.ShowMailStartEnd)
                        {
                            var msg = $"MAIL OK -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio}";
                            if (_tracer.ShowElapsedMs)
                                msg += $" ElapsedMs={swMail.ElapsedMilliseconds}";
                            _tracer.Write(msg);
                        }
                    }
                    catch (Exception ex)
                    {
                        swMail.Stop();

                        await _mailRepository.ActualizarEstadoAsync(
                            tenantConnection,
                            item.IdMailEnvio,
                            15,
                            ex.Message,
                            ct);

                        await _mailRepository.LogProcesoAsync(
                            tenantConnection,
                            item.IdMailEnvio,
                            "WORKER",
                            "ERROR",
                            ex.Message,
                            ex.ToString(),
                            ct);

                        var errMsg = $"MAIL ERROR -> Base={tenant.BaseDatos} IdMailEnvio={item.IdMailEnvio} Error={ex.Message}";
                        if (_tracer.ShowElapsedMs)
                            errMsg += $" ElapsedMs={swMail.ElapsedMilliseconds}";
                        _tracer.Write(errMsg);

                        _logger.LogError(ex, "Error enviando mail {IdMailEnvio} en base {BaseDatos}", item.IdMailEnvio, tenant.BaseDatos);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando tenant {BaseDatos}", tenant.BaseDatos);
                _tracer.Write($"TENANT ERROR -> Base={tenant.BaseDatos} Error={ex.Message}");
            }
            finally
            {
                swTenant.Stop();

                if (_tracer.ShowTenantStartEnd)
                {
                    var msg = $"TENANT END -> Base={tenant.BaseDatos}";
                    if (_tracer.ShowElapsedMs)
                        msg += $" ElapsedMs={swTenant.ElapsedMilliseconds}";
                    _tracer.Write(msg);
                }
            }
        }
    }
}