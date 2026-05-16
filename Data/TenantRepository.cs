using Dapper;
using MailDispatcher.Worker.Models;
using Microsoft.Data.SqlClient;

namespace MailDispatcher.Worker.Data;

public sealed class TenantRepository
{
    private readonly string _connectionString;

    public TenantRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("Master")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:Master");
    }

    public async Task<IReadOnlyList<TenantConfig>> ObtenerTenantsActivosAsync(CancellationToken ct)
    {
        const string sql = @"
SELECT
    Grupo,
    Empresa,
    BaseDatos,
    URLSistema,
    IdEstado,
    MailHabilitado,
    MailOrdenProceso,
    MailInlineImagesPath,
    MailSqlInstance
FROM dbo.GRPGruposEmpresas
WHERE IdEstado = 1
  AND MailHabilitado = 1
  AND ISNULL(BaseDatos, '') <> ''
ORDER BY MailOrdenProceso, Grupo, Empresa;";

        await using var cn = new SqlConnection(_connectionString);

        var rows = await cn.QueryAsync<TenantConfig>(
            new CommandDefinition(
                sql,
                commandTimeout: 60,
                cancellationToken: ct));

        return rows.ToList();
    }

    public async Task LogTenantAsync(
    TenantConfig tenant,
    string evento,
    int? cantidadMailsTomados,
    string? mensaje,
    CancellationToken ct)
{
    const string sql = @"
INSERT INTO dbo.MailDispatcherTenantLog
(
    Fecha,
    Grupo,
    Empresa,
    BaseDatos,
    Evento,
    CantidadMailsTomados,
    Mensaje,
    HostName
)
VALUES
(
    GETDATE(),
    @Grupo,
    @Empresa,
    @BaseDatos,
    @Evento,
    @CantidadMailsTomados,
    @Mensaje,
    @HostName
);";

    await using var cn = new SqlConnection(_connectionString);

    await cn.ExecuteAsync(new CommandDefinition(
        sql,
        new
        {
            tenant.Grupo,
            tenant.Empresa,
            tenant.BaseDatos,
            Evento = evento,
            CantidadMailsTomados = cantidadMailsTomados,
            Mensaje = mensaje,
            HostName = Environment.MachineName
        },
        cancellationToken: ct,
        commandTimeout: 30));
}
}