using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.OrderUseCaseRequirements;

public class FullyDeliveredRequirements : IOrderRequirement
{
    private string _failureReason = String.Empty; 
    public bool Fulfils(Order order)
    {
        if (order.OrderLines.Count == 0)
        {
            _failureReason = "No order lines were found in that order";
            return false;
        }

        if (order.OrderLines.All(line =>
                line.DeliveredQuantity.HasValue &&
                line.DeliveredQuantity >= line.OrderedQuantity))
        {
            return true;
        }
        else
        {
            _failureReason = "All order lines must be fully delivered";
            return false;
        }
    }
    
    public string FailureReason => _failureReason;
}