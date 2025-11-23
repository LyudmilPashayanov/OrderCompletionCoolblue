namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter.Databases;

public interface ISqlDialect
{
    string GetOrderIdsQuery();
    string GetOrderLinesQuery();
    string GetUpdateOrderStateQuery();
}