namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public interface ISqlDialect
{
    string GetOrderIdsQuery();
    string GetOrderLinesQuery();
    string GetUpdateOrderStateQuery();
}