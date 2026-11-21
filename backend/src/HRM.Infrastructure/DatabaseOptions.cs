using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace HRM.Next.Infrastructure;

public sealed class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class DatabaseOptionsValidator : IValidateOptions<DatabaseOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseOptions options)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(options.ConnectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource) ||
                !builder.InitialCatalog.StartsWith("HRM_Next_", StringComparison.OrdinalIgnoreCase))
                return ValidateOptionsResult.Fail("ConnectionStrings:Hrm must specify a server and a dedicated HRM_Next_ database.");
            return ValidateOptionsResult.Success;
        }
        catch (ArgumentException)
        {
            return ValidateOptionsResult.Fail("ConnectionStrings:Hrm is not a valid SQL Server connection configuration.");
        }
    }
}
