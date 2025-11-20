using Microsoft.Extensions.Options;
using OrderCompletion.Api.Ports;
using Polly;

namespace OrderCompletion.Api.Adapters.NotificationAdapter;

public static class NotificationAdapter
{
    public static void RegisterNotificationAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<NotificationAdapterSettings>()
            .Bind(configuration.GetSection("NotificationClientSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<INotificationClient, NotificationClient>();

        services.AddHttpClient<INotificationClient, NotificationClient>((serviceProvider, client) =>
        {
            NotificationAdapterSettings notificationSettings =
                serviceProvider.GetRequiredService<IOptions<NotificationAdapterSettings>>().Value;
            client.BaseAddress = new Uri(notificationSettings.NotificationsAddress);
        });

        services.AddSingleton<IAsyncPolicy>(serviceProvider =>
        {
            NotificationAdapterSettings notificationSettings =
                serviceProvider.GetRequiredService<IOptions<NotificationAdapterSettings>>().Value;
            return Policy
                .Handle<HttpRequestException>() // retry on HTTP failures
                .Or<TaskCanceledException>() // retry on timeouts
                .WaitAndRetryAsync(
                    retryCount: notificationSettings.RetryCount,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(notificationSettings.RetrySecondsTimeout)
                );
        });
    }
}