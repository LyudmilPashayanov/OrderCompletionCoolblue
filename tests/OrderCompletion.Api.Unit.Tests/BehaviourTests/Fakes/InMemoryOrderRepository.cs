using OrderCompletion.Api.Models;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Unit.Tests.BehaviourTests.Fakes;

public class InMemoryOrderRepository : IOrderCompletionRepository
{
    private readonly Dictionary<int, Order> _store = new Dictionary<int, Order>();
    
    public void Populate(Order order) => _store[order.Id] = Clone(order);
    
    public Task<Order?> GetOrdersByIdAsync(int orderId, CancellationToken ct = default)
    {
        _store.TryGetValue(orderId, out var order);
        return Task.FromResult(order is null ? null : Clone(order)); 
    }

    public Task<bool> CompleteOrderAsync(int orderId, CancellationToken ct = default)
    {
        if (!_store.TryGetValue(orderId, out var order)) return Task.FromResult(false);
        // mutate stored order to represent DB update
        order.OrderState = OrderState.Finished;
        return Task.FromResult(true);
    }
    
    /// <summary>
    /// Use a clone of an Order so that different tests running parallel don't modify and work with the same referenced object. 
    /// </summary>
    private static Order Clone(Order src) => new Order
    {
        Id = src.Id,
        OrderState = src.OrderState,
        OrderDate = src.OrderDate,
        OrderLines = src.OrderLines?.Select(ol => new OrderLine
        {
            ProductId = ol.ProductId,
            OrderedQuantity = ol.OrderedQuantity,
            DeliveredQuantity = ol.DeliveredQuantity
        }).ToArray()
    };
}