using OrderCompletion.Api.Utilities;

namespace OrderCompletion.Api.Unit.Tests.BehaviourTests.Fakes;

public class FakeClock : ISystemClock
{
    public DateTime UtcNow { get; }

    public FakeClock(DateTime utcNow)
    {
        UtcNow = utcNow;
    }
}