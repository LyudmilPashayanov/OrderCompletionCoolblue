namespace OrderCompletion.Api;

public struct NotificationResults
{
    public List<int> SuccessfullyNotifiedOrders;
    public List<int> UnsuccessfullyNotifiedOrders;

    public NotificationResults()
    {
        SuccessfullyNotifiedOrders = new List<int>();
        UnsuccessfullyNotifiedOrders = new List<int>();
    }
}