using MySql.Data.MySqlClient;

public interface IMySqlConnectionFactory
{
    MySqlConnection CreateConnection();
}