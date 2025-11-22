using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.OrderUseCaseRequirements;

public class OrderNotFinishedRequirement : IOrderRequirement
{
    public bool Fulfils(Order order)
    {
        return order.OrderState == OrderState.Submitted;
    }

    public string FailureReason => "Order has already been marked as finished.";
}