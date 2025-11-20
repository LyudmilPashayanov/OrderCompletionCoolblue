namespace OrderCompletion.Api.Adapters.NotificationAdapter;

public class NotificationAdapterSettings
{
    public string NotificationsAddress { get; set; } = "";
    public int RetrySecondsTimeout { get; set; } = 2;
    public int RetryCount { get; set; } = 5;
}