using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Unit.Tests.BehaviourTests.Fakes;

public class FakeNotificationClient : INotificationClient
{
    public readonly List<int> Notified = new List<int>(); 
    private readonly Func<int, CancellationToken, Task<bool>> _behaviour; 
    
    public FakeNotificationClient(Func<int, CancellationToken, Task<bool>> behaviour)
    {
        _behaviour = behaviour;
    }
    
    public Task<bool> NotifyOrderCompletedAsync(int orderId, CancellationToken ct)
    {
        Notified.Add(orderId);
        return _behaviour(orderId, ct);
    }
}