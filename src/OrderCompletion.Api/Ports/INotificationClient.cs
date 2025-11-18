namespace OrderCompletion.Api.Ports;

public interface INotificationClient
{
    void OrderCompleted(int orderId);
}