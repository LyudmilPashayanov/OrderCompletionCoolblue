using OrderCompletion.Api.Ports;

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

        services.AddSingleton<IMySqlConnectionFactory, MySqlConnectionFactory>();
        services.AddScoped<IOrderCompletionRepository, OrderCompletionRepository>();
    }
}