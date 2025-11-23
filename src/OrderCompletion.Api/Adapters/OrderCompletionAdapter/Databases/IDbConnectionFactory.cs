using System.Data.Common;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter.Databases;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}