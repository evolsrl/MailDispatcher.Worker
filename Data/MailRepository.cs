using System.Data;
using Dapper;
using MailDispatcher.Worker.Models;
using Microsoft.Data.SqlClient;

namespace MailDispatcher.Worker.Data;

public sealed class MailRepository
{
    public async Task<IReadOnlyList<MailQueueItem>> TomarPendientesAsync(
        string connectionString,
        int? idMailEnvio,
        int top,
        CancellationToken ct)
    {
        await using var cn = new SqlConnection(connectionString);

        var cmd = new CommandDefinition(
            "dbo.AudMailsEnviosTomarPendientes",
            new { IdMailEnvio = idMailEnvio, Top = top },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct,
            commandTimeout: 120);

        var rows = await cn.QueryAsync<MailQueueItem>(cmd);
        return rows.ToList();
    }

    public async Task<MailProfileConfig?> ObtenerConfigAsync(
        string connectionString,
        string profileName,
        CancellationToken ct)
    {
        await using var cn = new SqlConnection(connectionString);

        var cmd = new CommandDefinition(
            "dbo.AudMailsConfigObtenerPorProfile",
            new { ProfileName = profileName },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct,
            commandTimeout: 60);

        return await cn.QueryFirstOrDefaultAsync<MailProfileConfig>(cmd);
    }

    public async Task ActualizarEstadoAsync(
        string connectionString,
        int idMailEnvio,
        int idEstado,
        string? observacion,
        CancellationToken ct)
    {
        await using var cn = new SqlConnection(connectionString);

        var cmd = new CommandDefinition(
            "dbo.AudMailsEnviosActualizarEstado",
            new { IdMailEnvio = idMailEnvio, IdEstado = idEstado, Observacion = observacion },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct,
            commandTimeout: 60);

        await cn.ExecuteAsync(cmd);
    }

    public async Task LogProcesoAsync(
        string connectionString,
        int idMailEnvio,
        string proceso,
        string nivel,
        string? mensaje,
        string? detalleTecnico,
        CancellationToken ct)
    {
        await using var cn = new SqlConnection(connectionString);

        var cmd = new CommandDefinition(
            "dbo.AudMailsEnviosLogProceso",
            new
            {
                IdMailEnvio = idMailEnvio,
                Proceso = proceso,
                Nivel = nivel,
                Mensaje = mensaje,
                DetalleTecnico = detalleTecnico,
                HostName = Environment.MachineName,
                UsuarioProceso = Environment.UserName
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct,
            commandTimeout: 60);

        await cn.ExecuteAsync(cmd);
    }
}