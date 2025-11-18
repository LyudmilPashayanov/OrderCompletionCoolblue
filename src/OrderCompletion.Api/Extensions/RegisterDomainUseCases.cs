using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Extensions;

public static class DomainExtensions
{
    public static void RegisterDomainUseCases(this IServiceCollection services)
    {
        services.AddTransient<IOrderCompletionUseCase, OrderCompletionUseCase>();
    }
}