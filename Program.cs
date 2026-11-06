using MailDispatcher.Worker;
using MailDispatcher.Worker.Data;
using MailDispatcher.Worker.Options;
using MailDispatcher.Worker.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<DiagnosticOptions>(builder.Configuration.GetSection("Diagnostics"));
builder.Services.Configure<DestinatariosOptions>(builder.Configuration.GetSection("Destinatarios"));

// Fuera de Production (desarrollo, pruebas) las bases tienen correos reales: el filtro de destinatarios queda
// siempre activo aunque falte o este apagado en la configuracion. Sin Destinatarios:Permitidos no se envia nada.
var esProduccion = builder.Environment.IsProduction();
builder.Services.PostConfigure<DestinatariosOptions>(o =>
{
    if (!esProduccion)
        o.SoloPermitidos = true;
});

builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
});

builder.Services.AddSerilog();

builder.Services.AddSingleton<TenantRepository>();
builder.Services.AddSingleton<MailRepository>();
builder.Services.AddSingleton<TenantConnectionStringFactory>();
builder.Services.AddSingleton<MailComposer>();
builder.Services.AddSingleton<MailSender>();
builder.Services.AddSingleton<DiagnosticTracer>();

builder.Services.AddHostedService<Worker>();

builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Mail Dispatcher Worker";
});

try
{
    Log.Information("Iniciando Mail Dispatcher Worker");
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "El servicio terminó inesperadamente");
}
finally
{
    Log.CloseAndFlush();
}