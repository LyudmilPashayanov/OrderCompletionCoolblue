using Microsoft.Extensions.Logging.Abstractions;
using OrderCompletion.Api.Models;
using OrderCompletion.Api.OrderUseCaseRequirements;
using OrderCompletion.Api.Unit.Tests.BehaviourTests.Fakes;
using Polly;
using Xunit;

namespace OrderCompletion.Api.Unit.Tests.BehaviourTests;

public class OrderCompletionBehaviourTests
{
    [Fact]
    public async Task OldFullyDeliveredOrder_IsNotifiedAndMarkedFinished()
    {
        // Arrange
        var clock = new FakeClock(DateTime.UtcNow); 

        var repo = new InMemoryOrderRepository();
        repo.Populate( new Order {
            Id = 1, 
            OrderState = OrderState.Submitted,
            OrderDate = clock.UtcNow.AddYears(-1), 
            OrderLines = new []
            {
                new OrderLine(){Id = 1, OrderedQuantity = 10, DeliveredQuantity = 10, ProductId = 3},
                new OrderLine(){Id = 2, OrderedQuantity = 3, DeliveredQuantity = 3, ProductId = 5}
            }
        });

        var notification = new FakeNotificationClient((order, token) => Task.FromResult(true));
        
        var sut = new OrderCompletionUseCase(
            repo, 
            notification, 
            new IOrderRequirement[] 
                { 
                    new FullyDeliveredRequirements(), 
                    new OrderAgeRequirement(clock),
                    new OrderNotFinishedRequirement()
                },
            NullLogger<OrderCompletionUseCase>.Instance);

        // Act
        await sut.CompleteOrdersAsync(new[] { 1 }, CancellationToken.None);

        // Assert - read the same repository state and fake notifier
        var updated = await repo.GetOrdersByIdAsync(new[] {1}, CancellationToken.None);
        var order = updated.Single();

        Assert.Equal(OrderState.Finished, order.OrderState);
        Assert.Contains(1, notification.Notified);
    }
}