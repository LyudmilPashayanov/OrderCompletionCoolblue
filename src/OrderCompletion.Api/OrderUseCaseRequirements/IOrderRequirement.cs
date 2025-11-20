using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.OrderUseCaseRequirements;

public interface IOrderRequirement
{
    bool Fulfils(Order order);
    string FailureReason { get; }
}