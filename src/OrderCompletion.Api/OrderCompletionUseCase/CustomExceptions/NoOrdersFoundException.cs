namespace OrderCompletion.Api.CustomExceptions;

public class NoOrdersFoundException : Exception
{
    public IReadOnlyCollection<int> RequestedIds { get; }
    public NoOrdersFoundException(IReadOnlyCollection<int> requestedIds)
        : base($"No orders found for ids {string.Join(", ", requestedIds)}")
    {
        RequestedIds = requestedIds;
    }
}