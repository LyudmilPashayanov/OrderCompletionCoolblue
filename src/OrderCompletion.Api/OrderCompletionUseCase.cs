using OrderCompletion.Api.Models;
using OrderCompletion.Api.OrderUseCaseRequirements;
using OrderCompletion.Api.Ports;
using Polly;

namespace OrderCompletion.Api;

public class OrderCompletionUseCase : IOrderCompletionUseCase
{
    private readonly IOrderCompletionRepository _orderCompletionRepository;
    private readonly INotificationClient _notificationClient;
    private readonly IAsyncPolicy _notifyRetryPolicy;
    private readonly IEnumerable<IOrderRequirement> _requirements;

    public OrderCompletionUseCase(
        IOrderCompletionRepository orderCompletionRepository,
        INotificationClient notificationClient,
        IEnumerable<IOrderRequirement> requirements,
        IAsyncPolicy notifyRetryPolicy)
    {
        _orderCompletionRepository = orderCompletionRepository;
        _notificationClient = notificationClient;
        _requirements = requirements;
        _notifyRetryPolicy = notifyRetryPolicy;
    }

    public async Task CompleteOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct)
    {
        //TODO: Possible optimization- Check if 10,000 order Ids, split batches of 1000. Then for each batch ...
        
            List<Order> orders = await _orderCompletionRepository.GetOrdersByIdAsync(orderIds, ct);
            if (orders.Count == 0)
            {
                // no orders found. send an error.
                continue;
            }

            foreach (var order in orders)
            {
                if (VerifyOrderRequirements(order, _requirements) == false)
                {
                    continue; // Order doesn't fulfil all the requirements. 
                }
            }

            // Execute notification service
            bool isNotificationSuccessful = await ExecuteOrderNotificationAsync(orderId, ct);
            
            bool isSuccessfullyCompleted = false;
            
            if (isNotificationSuccessful)
            {
                isSuccessfullyCompleted = await _orderCompletionRepository.CompleteOrderAsync(orderId, ct);
            }
            
            // TODO: Populate the body for the API request with every order which failed or succeeded
        
    }

    private async Task<bool> ExecuteOrderNotificationAsync(int orderId, CancellationToken ct)
    { 
        bool notified = false;
        
        try
        {
            await _notifyRetryPolicy.ExecuteAsync(async (context, token) =>
            {
                //_logger.LogDebug("Notifying external service that Order {OrderId} is complete", orderId);
               notified = await _notificationClient.NotifyOrderCompletedAsync(orderId, token);
            }, new Context(), ct);
        }
        //TODO: Maybe have some custom exceptions.
        //catch (PermanentNotificationException pex)
        //{
        //    //_logger.LogWarning(pex, "Permanent failure notifying order {OrderId}; will not mark completed now.", orderId);
        //    notified = false;
        //}
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Notification failed for order {OrderId} after retries; leaving for later.", orderId);
            notified = false;
        }

        return notified;
    }
    
    private bool VerifyOrderRequirements(Order order, IEnumerable<IOrderRequirement> requirements)
    {
        foreach (var requirement in requirements)
        {
            if (requirement.Fulfils(order) == false)
            {
                return false;
            }
        }

        return true;
    }
}