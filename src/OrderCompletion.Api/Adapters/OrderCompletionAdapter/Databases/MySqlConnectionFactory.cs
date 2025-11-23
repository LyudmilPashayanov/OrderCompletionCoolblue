using System.Data.Common;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public class MySqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public MySqlConnectionFactory(IOptions<OrderCompletionAdapterSettings> options)
    {
        if (options?.Value == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _connectionString = options.Value.BuildConnectionString();
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("MySQL connection string is not configured.");
        }
    }

    public DbConnection CreateConnection() => new MySqlConnection(_connectionString);
}