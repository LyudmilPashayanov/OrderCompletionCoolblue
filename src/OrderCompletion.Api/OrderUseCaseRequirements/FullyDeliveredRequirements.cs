using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.OrderUseCaseRequirements;

public class FullyDeliveredRequirements : IOrderRequirement
{
    public bool Fulfils(Order order)
    {
        if (order.OrderLines.Count == 0)
        {
            //TODO: Special case where there is an order without any order lines. Maybe raise a special exception or add logging here?
            return false;
        }
        
        return order.OrderLines.All(line => 
            line.DeliveredQuantity.HasValue && 
            line.DeliveredQuantity == line.OrderedQuantity);
    }
    
    public string FailureReason => "All order lines must be fully delivered";
}