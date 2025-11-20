using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.OrderUseCaseRequirements;

public class OrderAgeRequirement : IOrderRequirement
{
    public bool Fulfils(Order order)
    {
        DateTime sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        return order.OrderDate <= sixMonthsAgo;
    }

    public string FailureReason => "Order must be at least 6 months old.";
}