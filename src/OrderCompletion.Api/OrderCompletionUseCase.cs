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
        foreach (var orderId in orderIds)
        {    
            ct.ThrowIfCancellationRequested(); // This will gracefully stop the loop, when the token has been canceled.
            
            Order? order = await GetOrderByIdAsync(orderId, ct);
            if (order == null)
            {
                continue; // Order doesn't exist or has already finished, skip it...
            }
           
            // Business logic to check if order can be completed
            if (VerifyOrderRequirements(order, _requirements) == false)
            {
                continue; // Order doesn't fulfil all the requirements. 
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
    }

    private async Task<bool> ExecuteOrderNotificationAsync(int orderId, CancellationToken ct)
    { 
        bool notified = false;
        try
        {
            await _notifyRetryPolicy.ExecuteAsync(async (context, token) =>
            {
                //_logger.LogDebug("Notifying external service that Order {OrderId} is complete", orderId);
                await _notificationClient.NotifyOrderCompletedAsync(orderId, token);
            }, new Context(), ct);

            notified = true;
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
    
    private async Task<Order?> GetOrderByIdAsync(int id, CancellationToken ct)
    {
        Order? order = await _orderCompletionRepository.GetOrderByIdAsync(id, ct);
        if (order == null)
        {
            // _logger.LogWarning("No order with OrderId:{OrderId} found in the repository.");
            return null;
        }
        if (order.OrderState == OrderState.Finished)
        {
            //_logger.LogInformation("Order {OrderId} already finished; skipping.", id);
            return null;
        }

        return order;
    }
}