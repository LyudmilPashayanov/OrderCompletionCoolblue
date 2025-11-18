using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.NotificationAdapter;

public class NotificationClient : INotificationClient
{
    public void OrderCompleted(int orderId)
    {
        throw new NotImplementedException();
    }
}