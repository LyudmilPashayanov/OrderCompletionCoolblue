namespace OrderCompletion.Api.Ports;

public interface IOrderCompletionUseCase
{
    void CompleteOrders(IReadOnlyCollection<int> orderIds);
}