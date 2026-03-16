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
        var rows = await cn.QueryAsync<TenantConfig>(new CommandDefinition(sql, cancellationToken: ct));
        return rows.ToList();
    }
}