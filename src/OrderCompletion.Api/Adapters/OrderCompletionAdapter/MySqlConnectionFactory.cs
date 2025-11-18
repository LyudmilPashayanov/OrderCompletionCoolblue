using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public class MySqlConnectionFactory : IMySqlConnectionFactory
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

    public MySqlConnection CreateConnection() => new MySqlConnection(_connectionString);
}