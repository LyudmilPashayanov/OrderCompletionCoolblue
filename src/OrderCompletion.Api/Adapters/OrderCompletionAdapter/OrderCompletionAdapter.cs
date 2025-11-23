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
        
        services.Configure<OrderCompletionAdapterSettings>(options =>
        {
            options.MySqlServerAddress = Environment.GetEnvironmentVariable("MYSQL_SERVER") 
                                         ?? options.MySqlServerAddress;
            options.MySqlUsername = Environment.GetEnvironmentVariable("MYSQL_USER") 
                                    ?? options.MySqlUsername;
            options.MySqlPassword = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") 
                                    ?? options.MySqlPassword;
        });
        
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IDbConnectionFactory, MySqlConnectionFactory>();
        services.AddSingleton<ISqlDialect, MySqlDialect>();
        services.AddScoped<IOrderCompletionRepository, OrderCompletionRepository>();
    }
}