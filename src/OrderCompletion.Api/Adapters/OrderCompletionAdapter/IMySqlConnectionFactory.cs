using MySql.Data.MySqlClient;

    // TODO: Check how we can make this interface more generic, so we can support different types of DB.
public interface IMySqlConnectionFactory
{
    MySqlConnection CreateConnection();
}