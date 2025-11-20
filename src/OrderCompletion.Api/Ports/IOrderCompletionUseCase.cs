namespace OrderCompletion.Api.Ports;

public interface IOrderCompletionUseCase
{
    Task CompleteOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct);
}