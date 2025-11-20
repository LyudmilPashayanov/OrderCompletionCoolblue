using OrderCompletion.Api.Models;
using OrderCompletion.Api.Utilities;

namespace OrderCompletion.Api.OrderUseCaseRequirements;

public class OrderAgeRequirement : IOrderRequirement
{
    private readonly ISystemClock _clock;

    public OrderAgeRequirement(ISystemClock clock)
    {
        _clock = clock;
    }
    
    public bool Fulfils(Order order)
    {
        DateTime sixMonthsAgo = _clock.UtcNow.AddMonths(-6);
        return order.OrderDate <= sixMonthsAgo;
    }

    public string FailureReason => "Order must be at least 6 months old.";
}