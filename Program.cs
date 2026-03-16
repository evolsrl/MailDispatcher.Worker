using MailDispatcher.Worker;
using MailDispatcher.Worker.Data;
using MailDispatcher.Worker.Options;
using MailDispatcher.Worker.Services;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Services.Configure<WorkerOptions>(builder.Configuration.GetSection("Worker"));
builder.Services.Configure<DiagnosticOptions>(builder.Configuration.GetSection("Diagnostics"));

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