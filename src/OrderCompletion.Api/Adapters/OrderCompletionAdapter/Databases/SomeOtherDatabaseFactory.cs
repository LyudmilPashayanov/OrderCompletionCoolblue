using System.Data.Common;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public class SomeOtherDatabaseFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SomeOtherDatabaseFactory(IOptions<OrderCompletionAdapterSettings> options)
    {
        if (options?.Value == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _connectionString = options.Value.BuildConnectionString();
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Some other DB connection string is not configured.");
        }
    }
    
    // Return the DbConnection of your other database
    public DbConnection CreateConnection() => new MySqlConnection(); 
}