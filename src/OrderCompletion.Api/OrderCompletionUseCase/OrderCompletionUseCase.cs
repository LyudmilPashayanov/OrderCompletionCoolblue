using OrderCompletion.Api.CustomExceptions;
using OrderCompletion.Api.Models;
using OrderCompletion.Api.OrderUseCaseRequirements;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api;

public class OrderCompletionUseCase : IOrderCompletionUseCase
{
    private readonly IOrderCompletionRepository _orderCompletionRepository;
    private readonly INotificationClient _notificationClient;
    private readonly IEnumerable<IOrderRequirement> _requirements;
    private readonly ILogger<OrderCompletionUseCase> _logger;

    public OrderCompletionUseCase(
        IOrderCompletionRepository orderCompletionRepository,
        INotificationClient notificationClient,
        IEnumerable<IOrderRequirement> requirements,
        ILogger<OrderCompletionUseCase> logger
        )
    {
        _orderCompletionRepository = orderCompletionRepository;
        _notificationClient = notificationClient;
        _requirements = requirements;
        _logger = logger;
    }
    
    //TODO: Possible optimization- Check if 10,000 order Ids, split batches of 1000. Then for each batch ...
    public async Task<CompleteOrdersResponse> CompleteOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct)
    {
        CompleteOrdersResponse response = new CompleteOrdersResponse();
        
        try
        {
            List<Order> orders = await GetOrdersAsync(orderIds, ct);

            NotificationResults notificationResults = await VerifyAndNotifyOrders(orders, ct);
            PopulateResponseNotificationResults(response, notificationResults);
            
            bool ordersSuccessfullyUpdated = await UpdateOrdersState(notificationResults.SuccessfullyNotifiedOrders, ct);
            PopulateResponseUpdateOrdersResults(response, notificationResults, ordersSuccessfullyUpdated);
        }
        catch (NoOrdersSuccessfullyNotifiedException ex)
        {
            _logger.LogError(ex.Message + ex.AttemptedOrderIds);
        }
        catch (NoOrdersSuccessfullyUpdated ex)
        {
            _logger.LogError(ex.Message);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Order completion cancelled by token for ids: {OrderIds}", orderIds);
        }
        catch (System.Data.Common.DbException dbEx)
        {
            _logger.LogError(dbEx, "Database error during order completion for ids: {OrderIds}", orderIds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during order completion for ids: {OrderIds}", orderIds);
        }
        
        return response;
    }

    private async Task<List<Order>> GetOrdersAsync(IReadOnlyCollection<int> orderIds, CancellationToken ct = default)
    {
        List<Order> orders = await _orderCompletionRepository.GetOrdersByIdAsync(orderIds, ct);
        if (orders.Count == 0)
        {
            _logger.LogWarning("No orders found with ids: {OrderIds}.", orderIds);
            throw new Exception("No orders found with ids.");
        }

        return orders;
    }

    private async Task<NotificationResults> VerifyAndNotifyOrders(List<Order> orders, CancellationToken ct = default)
    {
        NotificationResults notificationResults = new NotificationResults();
        
        foreach (var order in orders)
        {
            if (VerifyOrderRequirements(order, _requirements))
            {
                ct.ThrowIfCancellationRequested(); // If cancellation is triggered, this line will gracefully stop the loop.
                
                bool notifiedSuccessfully = await ExecuteOrdersNotificationAsync(order.Id, ct);
                
                if (notifiedSuccessfully)
                {
                    notificationResults.SuccessfullyNotifiedOrders.Add(order.Id);
                }
                else
                {
                    notificationResults.UnsuccessfullyNotifiedOrders.Add(order.Id);
                }
            }
        }

        if (notificationResults.SuccessfullyNotifiedOrders.Count == 0)
        {
            throw new NoOrdersSuccessfullyNotifiedException(notificationResults.UnsuccessfullyNotifiedOrders);
        }
        
        return notificationResults;
    }

    private async Task<bool> UpdateOrdersState(List<int> successfullyNotifiedOrders, CancellationToken ct)
    {
        bool successfullyUpdatedOrders = false;
        
        int ordersUpdated = await _orderCompletionRepository.CompleteOrdersAsync(successfullyNotifiedOrders, ct); 
        
        if (ordersUpdated == 0)
        {
            _logger.LogError("Orders notified, but not updated in the Database. Notifications sent for these orders is wrong.");
            throw new NoOrdersSuccessfullyUpdated();
        }
        else if (ordersUpdated == successfullyNotifiedOrders.Count)
        {
            successfullyUpdatedOrders = true;
        }
        else if (ordersUpdated <  successfullyNotifiedOrders.Count)
        {
            _logger.LogWarning("Orders notified, but not all updated in the Database.");
            successfullyUpdatedOrders = true;
        }
        
        return successfullyUpdatedOrders;
    }

    private async Task<bool> ExecuteOrdersNotificationAsync(int orderId, CancellationToken ct)
    {
        return await _notificationClient.NotifyOrderCompletedAsync(orderId, ct);
    }
    
    private bool VerifyOrderRequirements(Order order, IEnumerable<IOrderRequirement> requirements)
    {
        foreach (var requirement in requirements)
        {
            if (requirement.Fulfils(order) == false)
            {
                _logger.LogDebug("Requested order with Id {OrderId} does not meet requirement: {RequirementName} to be completed.", order.Id, requirement.GetType());
                return false;
            }
        }

        return true;
    }
    
    private void PopulateResponseNotificationResults(CompleteOrdersResponse responseToPopulate,
        NotificationResults notificationResults)
    {
        responseToPopulate.SuccessfullyNotifiedOrders = notificationResults.SuccessfullyNotifiedOrders;
        responseToPopulate.UnsuccessfullyNotifiedOrders = notificationResults.UnsuccessfullyNotifiedOrders;
    }
    
    private void PopulateResponseUpdateOrdersResults(CompleteOrdersResponse responseToPopulate, NotificationResults notificationResults, bool ordersSuccessfullyUpdated)
    {
        responseToPopulate.OrdersSuccessfullyUpdated = ordersSuccessfullyUpdated;
        if (!ordersSuccessfullyUpdated)
        {
            responseToPopulate.FailedToUpdateOrders = notificationResults.SuccessfullyNotifiedOrders;
        }
    }
}