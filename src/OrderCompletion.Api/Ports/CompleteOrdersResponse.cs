namespace OrderCompletion.Api.Ports;

public class CompleteOrdersResponse
{
    public List<int> SuccessfullyNotifiedOrders { get; set; } = new();
    public List<int> UnsuccessfullyNotifiedOrders { get; set; } = new();
    public bool OrdersSuccessfullyUpdated { get; set; }
    public List<int> FailedToUpdateOrders { get; set; } = new();  
}