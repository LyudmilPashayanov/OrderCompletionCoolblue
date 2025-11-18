using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.Ports;

public interface IOrderCompletionRepository
{
    Order GetOrderById(int orderId);

    void CompleteOrder(int orderId);
}