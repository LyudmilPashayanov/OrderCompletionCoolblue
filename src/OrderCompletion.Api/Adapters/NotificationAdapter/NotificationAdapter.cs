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

        services.AddHttpClient<INotificationClient, NotificationClient>((serviceProvider, client) =>
        {
            NotificationAdapterSettings notificationSettings =
                serviceProvider.GetRequiredService<IOptions<NotificationAdapterSettings>>().Value;
            client.BaseAddress = new Uri(notificationSettings.NotificationsAddress);
        });

        services.AddSingleton<IAsyncPolicy<HttpResponseMessage>>(serviceProvider =>
        {
            NotificationAdapterSettings notificationSettings =
                serviceProvider.GetRequiredService<IOptions<NotificationAdapterSettings>>().Value;

            return Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .Or<TaskCanceledException>()
                .OrResult(response => (int)response.StatusCode >= 500) // retry on 500, because notification service returns that on fail.
                .WaitAndRetryAsync(
                    retryCount: notificationSettings.RetryCount,
                    sleepDurationProvider: attempt => TimeSpan.FromSeconds(notificationSettings.RetrySecondsTimeout)
                );
        });
    }
}