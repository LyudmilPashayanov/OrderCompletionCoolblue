using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Adapters.NotificationAdapter;

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClient> _logger;
    public NotificationClient(HttpClient httpClient,  ILogger<NotificationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> NotifyOrderCompletedAsync(int orderId, CancellationToken ct)
    {
        var requestUri = $"notify/{orderId}";
        try
        {
            var response = await _httpClient.GetAsync(requestUri, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Notified completion for order {OrderId}", orderId);
                return true;
            }
            
            _logger.LogWarning("Notification for order {OrderId} returned {StatusCode}", orderId, response.StatusCode);
            return false; 
        }
        catch (HttpRequestException ex) // Client will handle retry 
        {
            _logger.LogError(ex, "HTTP failure notifying order {OrderId}", orderId);
            throw;
        }
        catch (TaskCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Notification request cancelled for order {OrderId}", orderId);
            throw;
        }
    }
}