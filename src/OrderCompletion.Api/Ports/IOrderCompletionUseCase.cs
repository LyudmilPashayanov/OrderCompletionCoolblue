namespace OrderCompletion.Api.Ports;

public interface IOrderCompletionUseCase
{
    Task<CompleteOrdersResponse> CompleteOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct);
}