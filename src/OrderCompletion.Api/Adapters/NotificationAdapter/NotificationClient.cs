using OrderCompletion.Api.Ports;
using Polly;

namespace OrderCompletion.Api.Adapters.NotificationAdapter;

public class NotificationClient : INotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationClient> _logger;
    private readonly IAsyncPolicy<HttpResponseMessage> _httpPolicy;

    public NotificationClient(HttpClient httpClient,  ILogger<NotificationClient> logger, IAsyncPolicy<HttpResponseMessage> httpPolicy)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpPolicy =  httpPolicy;
    }

    public async Task<bool> NotifyOrderCompletedAsync(int orderId, CancellationToken ct)
    {
        string requestUri = $"notify/{orderId}";
        
        try
        {
            // Policy retries on exceptions and 5xx results
            HttpResponseMessage response = await _httpPolicy.ExecuteAsync(
                (context, token) =>
                {
                    var req = new HttpRequestMessage(HttpMethod.Get, requestUri);
                    return _httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, token);
                },
                new Context(),
                ct);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            _logger.LogWarning("Notification for order {OrderId} returned non-success {StatusCode}", orderId, response.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogInformation("Notification request cancelled for order {OrderId}", orderId);
            throw;
        }
        catch (Exception ex)
        {
            // All retries exhausted or unexpected exception
            _logger.LogError(ex, "Notification failed for order {OrderId} after retries...", orderId);
            return false;
        }
    }
}