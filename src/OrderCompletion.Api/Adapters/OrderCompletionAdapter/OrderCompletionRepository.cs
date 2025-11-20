using OrderCompletion.Api.Models;
using Dapper;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Dtos;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Mappers;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

internal class OrderCompletionRepository : IOrderCompletionRepository
{
    private readonly IMySqlConnectionFactory _mySqlConnectionFactory;

    public OrderCompletionRepository(IMySqlConnectionFactory mySqlConnectionFactory)
    {
        _mySqlConnectionFactory = mySqlConnectionFactory;
    }
    
    //TODO: Add error handling.
    //TODO: Add logging
    public async Task<bool> CompleteOrderAsync(int orderId, CancellationToken ct)
    {
        await using var connection = _mySqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        
        const string query = @"
        UPDATE ORDERS
        SET OrderStateId = @OrderStateId
        WHERE Id = @Id
          AND OrderStateId != @OrderStateId;";

        var parameters = new
        {
            OrderStateId = (int)OrderState.Finished,
            Id = orderId
        };

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(query, parameters, cancellationToken: ct));

        // affected == 1 -> we successfully moved it to Finished just now
        // affected == 0 -> it was already Finished (or the row doesn't exist)
        return affected == 1;
    }

    //TODO: Add error handling.
    //TODO: Add logging
    public async Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        await using var connection = _mySqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        
        // fetch order DTO
        const string orderQuery = @"
        SELECT orders.Id, orders.OrderDate, orders.OrderStateId 
        FROM ORDERS orders 
        WHERE orders.Id = @Id;";
        
        OrderDto? orderDto = await connection.QuerySingleOrDefaultAsync<OrderDto>(
            new CommandDefinition(orderQuery, new { Id = orderId }, cancellationToken: ct));

        if (orderDto == null)
        {
            //TODO: Add error handling.
            return null;
        }

        // fetch order lines
        const string orderLinesQuery = @"
        SELECT line.Id, line.ProductId, line.OrderedQuantity, line.DeliveredQuantity 
        FROM ORDER_LINES line 
        WHERE OrderId = @OrderId";
        
        List<OrderLineDto> orderLines = (await connection.QueryAsync<OrderLineDto>(
                new CommandDefinition(orderLinesQuery, new { OrderId = orderId }, cancellationToken: ct)))
            .AsList();

        orderDto.OrderLines = orderLines;
        return orderDto.ToDomain();
    }
}