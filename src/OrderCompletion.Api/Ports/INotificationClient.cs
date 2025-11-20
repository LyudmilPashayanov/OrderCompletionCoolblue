namespace OrderCompletion.Api.Ports;

public interface INotificationClient
{
    Task<bool> NotifyOrderCompletedAsync(int orderId, CancellationToken ct);
}