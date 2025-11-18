using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api;

public class OrderCompletionUseCase : IOrderCompletionUseCase
{
    private readonly IOrderCompletionRepository _orderCompletionRepository;
    private readonly INotificationClient _notificationClient;

    public OrderCompletionUseCase(
        IOrderCompletionRepository orderCompletionRepository,
        INotificationClient notificationClient)
    {
        _orderCompletionRepository = orderCompletionRepository;
        _notificationClient = notificationClient;
    }

    public void CompleteOrders(IReadOnlyCollection<int> orderIds) =>
        throw new NotImplementedException();
}