namespace OrderCompletion.Api.Utilities;

public interface ISystemClock
{
    DateTime UtcNow { get; }
}