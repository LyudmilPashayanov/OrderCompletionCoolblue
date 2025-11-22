using OrderCompletion.Api.Models;
using Dapper;
using MySql.Data.MySqlClient;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Dtos;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Mappers;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

internal class OrderCompletionRepository : IOrderCompletionRepository
{
    private readonly IMySqlConnectionFactory _mySqlConnectionFactory;
    private readonly ILogger<OrderCompletionRepository> _logger;

    public OrderCompletionRepository(IMySqlConnectionFactory mySqlConnectionFactory,  ILogger<OrderCompletionRepository> Logger)
    {
        _mySqlConnectionFactory = mySqlConnectionFactory;
        _logger = Logger;
    }
    
    public async Task<List<Order>> GetOrdersByIdAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
    {
        if (orderIds.Count == 0)
        {
            return new List<Order>();
        }

        try
        {
            await using var connection = _mySqlConnectionFactory.CreateConnection();
            await connection.OpenAsync(ct);

            List<OrderDto> orderDtos = await QueryOrders(orderIds, connection, ct);

            if (orderDtos.Count == 0)
            {
                _logger.LogInformation("GetOrdersByIdAsync: No orders found with these orderIds: {OrderIds}", orderIds);
                return new List<Order>();
            }

            Dictionary<int, List<OrderLineDto>> linesPerOrder = await QueryLinesPerOrder(orderIds, connection, ct);

            List<Order> result = new List<Order>(orderDtos.Count);
            foreach (OrderDto orderDto in orderDtos)
            {
                linesPerOrder.TryGetValue(orderDto.Id, out var linesForOrder);
                orderDto.OrderLines = linesForOrder ?? new List<OrderLineDto>();
                result.Add(orderDto.ToDomain());
            }

            return result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) // called when cancelled from above.
        {
            _logger.LogWarning("GetOrdersByIdAsync cancelled by token for ids: {OrderIds}", orderIds);
            throw;
        }
        catch (MySqlException mex)
        {
            _logger.LogError(mex, "MySql Database error while fetching orders for ids: {OrderIds}", orderIds);
            throw ;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching orders for ids: {OrderIds}", orderIds);
            throw;
        }    
    }

    private async Task<List<OrderDto>> QueryOrders(IReadOnlyCollection<int> orderIds, MySqlConnection connection, CancellationToken ct)
    {
        const string orderQuery = @"
        SELECT o.Id, o.OrderDate, o.OrderStateId
        FROM ORDERS o
        WHERE o.Id IN @Ids;";

        List<OrderDto> orderDtos = (await connection.QueryAsync<OrderDto>(
                new CommandDefinition(orderQuery, new { Ids = orderIds }, cancellationToken: ct)))
            .AsList();

        return orderDtos;
    }

    private async Task<Dictionary<int, List<OrderLineDto>>> QueryLinesPerOrder(IReadOnlyCollection<int> orderIds,
        MySqlConnection connection, CancellationToken ct)
    {
        const string orderLinesQuery = @"
        SELECT l.Id, l.OrderId, l.ProductId, l.OrderedQuantity, l.DeliveredQuantity
        FROM ORDER_LINES l
        WHERE l.OrderId IN @Ids;";

        List<OrderLineDto> orderLineDtos = (await connection.QueryAsync<OrderLineDto>(
                new CommandDefinition(orderLinesQuery, new { Ids = orderIds }, cancellationToken: ct)))
            .AsList();

        Dictionary<int, List<OrderLineDto>> linesPerOrder = new Dictionary<int, List<OrderLineDto>>();
        foreach (OrderLineDto orderLineDto in orderLineDtos)
        {
            if (linesPerOrder.TryGetValue(orderLineDto.OrderId, out var orderLines))
            {
                orderLines.Add(orderLineDto);
            }
            else
            {
                linesPerOrder.Add(orderLineDto.OrderId, new List<OrderLineDto>() { orderLineDto });
            }
        }

        return linesPerOrder;
    }

    public async Task<int> CompleteOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct)
    {
        if (orderIds.Count == 0)
        {
            return 0;
        }

        try
        {
            await using var connection = _mySqlConnectionFactory.CreateConnection();
            await connection.OpenAsync(ct);

            const string query = @"
                UPDATE ORDERS
                SET OrderStateId = @FinishedState
                WHERE Id IN @Ids
                  AND OrderStateId = @SubmittedState;";    

            var parameters = new
            {
                FinishedState = (int)OrderState.Finished,
                SubmittedState = (int)OrderState.Submitted,
                Ids = orderIds.ToArray()
            };

            int affected = await connection.ExecuteAsync(
                new CommandDefinition(query, parameters, cancellationToken: ct));

            _logger.LogInformation("CompleteOrdersAsync: updated {Affected} orders to Finished out of {Requested}", affected, orderIds.Count);
            return affected;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) // reached when cancelled from above.
        {
            _logger.LogWarning("CompleteOrdersAsync cancelled by token for ids: {OrderIds}", orderIds);
            return 0;
        }
        catch (MySqlException mex)
        {
            _logger.LogError(mex, "MySql Database error while changing order states for order ids: {OrderIds}", orderIds);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while changing order states for order ids: {OrderIds}", orderIds);
            return 0;
        }
    }
}