using OrderCompletion.Api.Ports;
using OrderCompletion.Api.Utilities;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public static class OrderCompletionAdapter
{
    public static void RegisterOrderCompletionAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<OrderCompletionAdapterSettings>()
            .Bind(configuration.GetSection("OrderCompletionAdapterSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();
        services.AddScoped<IOrderCompletionRepository, OrderCompletionRepository>();
    }
}