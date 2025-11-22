using OrderCompletion.Api.OrderUseCaseRequirements;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Extensions;

public static class DomainExtensions
{
    public static void RegisterDomainUseCases(this IServiceCollection services)
    {        
        services.AddSingleton<IOrderRequirement, FullyDeliveredRequirements>();
        services.AddSingleton<IOrderRequirement, OrderAgeRequirement>();
        services.AddSingleton<IOrderRequirement, OrderNotFinishedRequirement>();
        
        services.AddScoped<IOrderCompletionUseCase, OrderCompletionUseCase>();
    }
}