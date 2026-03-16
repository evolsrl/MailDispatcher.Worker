namespace MailDispatcher.Worker.Services;

public sealed class TenantConnectionStringFactory
{
    private readonly IConfiguration _configuration;

    public TenantConnectionStringFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Build(string databaseName, string? sqlInstanceOverride = null)
    {
        var template = _configuration.GetConnectionString("TenantTemplate")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:TenantTemplate");

        var result = template.Replace("{DATABASE}", databaseName, StringComparison.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(sqlInstanceOverride))
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(result)
            {
                DataSource = sqlInstanceOverride
            };
            result = builder.ConnectionString;
        }

        return result;
    }
}