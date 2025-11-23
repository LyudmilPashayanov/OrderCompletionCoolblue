using System.Data.Common;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public interface IDbConnectionFactory
{
    DbConnection CreateConnection();
}