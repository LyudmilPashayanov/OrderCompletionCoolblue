using OrderCompletion.Api.Models;
using Dapper;
using MySql.Data.MySqlClient;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Dtos;
using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Mappers;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

internal class OrderCompletionRepository : IOrderCompletionRepository
{
    private readonly string _connectionString;

    public OrderCompletionRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public void CompleteOrder(int orderId)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            var query = "UPDATE ORDERS SET OrderStateId = @OrderStateId WHERE Id = @Id";
            connection.Execute(query, new { OrderStateId = (int)OrderState.Finished, Id = orderId });
        }
    }

    public Order GetOrderById(int orderId)
    {
        using (var connection = new MySqlConnection(_connectionString))
        {
            connection.Open();

            var orderQuery = "SELECT * FROM ORDERS WHERE Id = @Id";
            var orderDto = connection.QuerySingleOrDefault<OrderDto>(orderQuery, new { Id = orderId });

            if (orderDto == null) return null;

            var orderLinesQuery = "SELECT * FROM ORDER_LINES WHERE OrderId = @OrderId";
            var orderLines = connection.Query<OrderLineDto>(orderLinesQuery, new { OrderId = orderId }).ToList();

            orderDto.OrderLines = orderLines;

            return orderDto.ToDomain();
        }
    }
}