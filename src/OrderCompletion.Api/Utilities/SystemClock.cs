namespace OrderCompletion.Api.Utilities;

public class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}