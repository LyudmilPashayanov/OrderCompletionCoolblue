using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.Ports;

public interface IOrderCompletionRepository
{
    public Task<List<Order>> GetOrdersByIdAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default);

    public Task<int> CompleteOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default);
}