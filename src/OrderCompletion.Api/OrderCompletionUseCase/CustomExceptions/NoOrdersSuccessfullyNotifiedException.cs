namespace OrderCompletion.Api.CustomExceptions;

public sealed class NoOrdersSuccessfullyNotifiedException : Exception
{
    public VerificationAndNotificationResults FailedResults { get; }

    public NoOrdersSuccessfullyNotifiedException(
        VerificationAndNotificationResults failedResults)
        : base($"No orders were successfully notified{string.Join(", ", failedResults.UnsuccessfullyNotifiedOrders)}")
    {
        FailedResults = failedResults;
    }
}
