using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.NotificationAdapter;

public static class NotificationAdapter
{
    public static void RegisterNotificationAdapter(this IServiceCollection services)
    {
        services.AddTransient<INotificationClient, NotificationClient>();
    }
}