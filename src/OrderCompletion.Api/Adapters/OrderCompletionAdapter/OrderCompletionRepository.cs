using OrderCompletion.Api.Models;
using Dapper;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Dtos;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Mappers;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

internal class OrderCompletionRepository : IOrderCompletionRepository
{
    private readonly IMySqlConnectionFactory _mySqlConnectionFactory;

    public OrderCompletionRepository(IMySqlConnectionFactory connectionString)
    {
        _mySqlConnectionFactory = connectionString;
    }

    // TODO: Check how to do this query idempotent
    public async Task<bool> TryCompleteOrderAsync(int orderId, CancellationToken ct)
    {
        await using var connection = _mySqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(ct);

        const string query = @"
        UPDATE ORDERS
        SET OrderStateId = @OrderStateId
        WHERE Id = @Id";

        var parameters = new
        {
            OrderStateId = (int)OrderState.Finished,
            Id = orderId
        };

        var affected = await connection.ExecuteAsync(
            new CommandDefinition(query, parameters, cancellationToken: ct));

            return affected == 1;
    }

    public async Task<Order> GetOrderByIdAsync(int orderId, CancellationToken ct = default)
    {
        await using var connection = _mySqlConnectionFactory.CreateConnection();
        await connection.OpenAsync(ct);
        
        // fetch order DTO
        const string orderQuery = "SELECT * FROM ORDERS WHERE Id = @Id";
        var orderDto = await connection.QuerySingleOrDefaultAsync<OrderDto>(
            new CommandDefinition(orderQuery, new { Id = orderId }, cancellationToken: ct));

        if (orderDto == null)
            return null;

        // fetch order lines
        const string orderLinesQuery = "SELECT * FROM ORDER_LINES WHERE OrderId = @OrderId";
        var orderLines = (await connection.QueryAsync<OrderLineDto>(
                new CommandDefinition(orderLinesQuery, new { OrderId = orderId }, cancellationToken: ct)))
            .AsList();

        orderDto.OrderLines = orderLines;
        return orderDto.ToDomain();
    }
}