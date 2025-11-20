using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.Ports;

public interface IOrderCompletionRepository
{
    public Task<Order?> GetOrderByIdAsync(int orderId, CancellationToken ct = default);

    public Task<bool> CompleteOrderAsync(int orderId, CancellationToken ct = default);
}