using MySql.Data.MySqlClient;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public class OrderCompletionAdapterSettings
{
    public string MySqlServerAddress { get; set; } = "localhost";
    public int MySqlServerPort { get; set; } = 3306;
    public string MySqlDatabase { get; set; } = "";
    public string MySqlUsername { get; set; } = "";
    public string MySqlPassword { get; set; } = "";

    public string BuildConnectionString()
    {
        var b = new MySqlConnectionStringBuilder
        {
            Server = MySqlServerAddress,
            Port = (uint) MySqlServerPort,
            Database = MySqlDatabase,
            UserID = MySqlUsername,
            Password = MySqlPassword,
        };
        
        return b.ConnectionString;
    }
}