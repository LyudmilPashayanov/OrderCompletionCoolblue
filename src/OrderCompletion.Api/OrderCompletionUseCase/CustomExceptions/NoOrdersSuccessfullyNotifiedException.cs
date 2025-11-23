namespace OrderCompletion.Api.CustomExceptions;

public sealed class NoOrdersSuccessfullyNotifiedException : Exception
{
    public List<int> AttemptedOrderIds { get; }

    public NoOrdersSuccessfullyNotifiedException(
        List<int> attemptedOrderIds)
        : base($"No orders were successfully notified{string.Join(", ", attemptedOrderIds)}")
    {
        AttemptedOrderIds = attemptedOrderIds;
    }
}
