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

            VerificationAndNotificationResults verificationAndNotificationResults = await VerifyAndNotifyOrders(orders, ct);
            PopulateResponseNotificationResults(response, verificationAndNotificationResults);
            
            bool ordersSuccessfullyUpdated = await UpdateOrdersState(verificationAndNotificationResults.SuccessfullyNotifiedOrders, ct);
            PopulateResponseUpdateOrdersResults(response, verificationAndNotificationResults, ordersSuccessfullyUpdated);
        }
        catch (NoOrdersFoundException ex)
        {
            _logger.LogWarning(ex, "No orders found for ids: {OrderIds}", ex.RequestedIds);
            response.UnsuccessfullyNotifiedOrders = ex.RequestedIds.ToList();
        }
        catch (NoOrdersSuccessfullyNotifiedException ex)
        {
            _logger.LogError(ex.Message);
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
            throw new NoOrdersFoundException(orderIds);
        }

        return orders;
    }

    private async Task<VerificationAndNotificationResults> VerifyAndNotifyOrders(List<Order> orders, CancellationToken ct = default)
    {
        VerificationAndNotificationResults verificationAndNotificationResults = new VerificationAndNotificationResults();
        
        foreach (var order in orders)
        {
            if (VerifyOrderRequirements(order, _requirements))
            {
                ct.ThrowIfCancellationRequested(); // If cancellation is triggered, this line will gracefully stop the loop.
                
                bool notifiedSuccessfully = await ExecuteOrdersNotificationAsync(order.Id, ct);
                
                if (notifiedSuccessfully)
                {
                    verificationAndNotificationResults.SuccessfullyNotifiedOrders.Add(order.Id);
                }
                else
                {
                    verificationAndNotificationResults.UnsuccessfullyNotifiedOrders.Add(order.Id);
                }
            }
            else
            {
                verificationAndNotificationResults.FailedRequirementsOrders.Add(order.Id);
                _logger.LogWarning("Order with Id: {OrderId}, failed to meet all requirements and will not be marked as completed.",order.Id);
            }
        }

        if (verificationAndNotificationResults.SuccessfullyNotifiedOrders.Count == 0)
        {
            throw new NoOrdersSuccessfullyNotifiedException(verificationAndNotificationResults.UnsuccessfullyNotifiedOrders);
        }
        
        return verificationAndNotificationResults;
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
                _logger.LogWarning("Requested order with Id {OrderId} does not meet requirement- Failure reason: {Reason}.", order.Id, requirement.FailureReason);
                return false;
            }
        }

        return true;
    }
    
    private void PopulateResponseNotificationResults(CompleteOrdersResponse responseToPopulate,
        VerificationAndNotificationResults verificationAndNotificationResults)
    {
        responseToPopulate.SuccessfullyNotifiedOrders = verificationAndNotificationResults.SuccessfullyNotifiedOrders;
        responseToPopulate.UnsuccessfullyNotifiedOrders = verificationAndNotificationResults.UnsuccessfullyNotifiedOrders;
        responseToPopulate.FailedToUpdateOrders = verificationAndNotificationResults.FailedRequirementsOrders;
    }
    
    private void PopulateResponseUpdateOrdersResults(CompleteOrdersResponse responseToPopulate, VerificationAndNotificationResults verificationAndNotificationResults, bool ordersSuccessfullyUpdated)
    {
        responseToPopulate.OrdersSuccessfullyUpdated = ordersSuccessfullyUpdated;
        if (!ordersSuccessfullyUpdated)
        {
            responseToPopulate.FailedToUpdateOrders = verificationAndNotificationResults.SuccessfullyNotifiedOrders;
        }
    }
}