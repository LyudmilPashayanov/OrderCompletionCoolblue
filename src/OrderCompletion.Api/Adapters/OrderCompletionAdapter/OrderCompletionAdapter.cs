using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter;

public static class OrderCompletionAdapter
{
    public static void RegisterOrderCompletionAdapter(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection("OrderCompletionAdapterSettings").Get<OrderCompletionAdapterSettings>();
        var connectionString = $"Server={settings.MySqlServerAddress};Port={settings.MySqlServerPort};Database={settings.MySqlDatabase};User Id={settings.MySqlUsername};Password={settings.MySqlPassword};";

        services.AddTransient<IOrderCompletionRepository>((_) => new OrderCompletionRepository(connectionString));
    }
}