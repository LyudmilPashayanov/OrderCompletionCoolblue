namespace OrderCompletion.Api;

public struct VerificationAndNotificationResults
{
    public List<int> SuccessfullyNotifiedOrders;
    public List<int> UnsuccessfullyNotifiedOrders;
    public List<int> FailedRequirementsOrders;

    public VerificationAndNotificationResults()
    {
        SuccessfullyNotifiedOrders = new List<int>();
        UnsuccessfullyNotifiedOrders = new List<int>();
        FailedRequirementsOrders = new List<int>();
    }
}