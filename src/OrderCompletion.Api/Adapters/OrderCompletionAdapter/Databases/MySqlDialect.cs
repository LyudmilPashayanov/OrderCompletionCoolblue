namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public class MySqlDialect : ISqlDialect
{
    public string GetOrderIdsQuery()
    {
        return @"
        SELECT o.Id, o.OrderDate, o.OrderStateId
        FROM ORDERS o
        WHERE o.Id IN @Ids;";
    }

    public string GetOrderLinesQuery()
    {
        return @"
        SELECT l.Id, l.OrderId, l.ProductId, l.OrderedQuantity, l.DeliveredQuantity
        FROM ORDER_LINES l
        WHERE l.OrderId IN @Ids;";
    }

    public string GetUpdateOrderStateQuery()
    {
        return @"
                UPDATE ORDERS
                SET OrderStateId = @FinishedState
                WHERE Id IN @Ids
                  AND OrderStateId = @SubmittedState;";
    }
}