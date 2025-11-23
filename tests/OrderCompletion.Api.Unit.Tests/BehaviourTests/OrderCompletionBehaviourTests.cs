using Microsoft.Extensions.Logging.Abstractions;
using OrderCompletion.Api.Models;
using OrderCompletion.Api.OrderUseCaseRequirements;
using OrderCompletion.Api.Unit.Tests.BehaviourTests.Fakes;
using Xunit;
using OrderCompletion.Api.Utilities;

namespace OrderCompletion.Api.Unit.Tests.BehaviourTests;

public class OrderCompletionBehaviourTests
{
    [Fact]
    public async Task SingleOrder_OldFullyDelivered_IsNotifiedAndMarkedFinishedAndResponseCorrect()
    {
        // Arrange
        var clock = new FakeClock(DateTime.UtcNow); 

        var repo = new InMemoryOrderRepository();
        repo.Populate(GetOldCompletedOrder(clock, 1));

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
        var response = await sut.CompleteOrdersAsync(new[] { 1 }, CancellationToken.None);

        // Assert
        var updated = await repo.GetOrdersByIdAsync(new[] {1}, CancellationToken.None);
        var order = updated.Single();
        
        Assert.Equal(OrderState.Finished, order.OrderState);
        Assert.Contains(1, notification.Notified);

        Assert.True(response.OrdersSuccessfullyUpdated);
        Assert.Contains(1, response.SuccessfullyNotifiedOrders);
    }
    
    [Fact]
    public async Task MultipleOrders_CorrectAndWrong_OnlyCorrectAreNotifiedAndMarkedFinishedAndResponseCorrect()
    {
        // Arrange
        var clock = new FakeClock(DateTime.UtcNow); 

        var repo = new InMemoryOrderRepository();
        repo.Populate(GetOldCompletedOrder(clock, 1));
        repo.Populate(GetOldIncompletedOrder(clock, 2));
        repo.Populate(GetRecentCompletedOrder(clock, 3));
        repo.Populate(GetOldCompletedOrder(clock, 4));

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
        var response = await sut.CompleteOrdersAsync(new[] { 1,2,3,4 }, CancellationToken.None);

        // Assert
        var updated = await repo.GetOrdersByIdAsync(new[] { 1, 2, 3, 4 }, CancellationToken.None);

// Order 1: old, fully delivered: should be finished and notified
        var order1 = updated.Single(o => o.Id == 1);
        Assert.Equal(OrderState.Finished, order1.OrderState);
        Assert.Contains(1, notification.Notified);

// Order 2: old, incomplete: should remain submitted and not notified
        var order2 = updated.Single(o => o.Id == 2);
        Assert.Equal(OrderState.Submitted, order2.OrderState);
        Assert.DoesNotContain(2, notification.Notified);

// Order 3: recent, fully delivered: should remain submitted and not notified
        var order3 = updated.Single(o => o.Id == 3);
        Assert.Equal(OrderState.Submitted, order3.OrderState);
        Assert.DoesNotContain(3, notification.Notified);

// Order 4: old, fully delivered: should be finished and notified
        var order4 = updated.Single(o => o.Id == 4);
        Assert.Equal(OrderState.Finished, order4.OrderState);
        Assert.Contains(4, notification.Notified);

// Assert response object
        Assert.True(response.OrdersSuccessfullyUpdated);
        Assert.Equal(new[] { 1, 4 }, response.SuccessfullyNotifiedOrders.OrderBy(x => x));
        Assert.Empty(response.UnsuccessfullyNotifiedOrders);
        Assert.Contains(2, response.FailedToUpdateOrders);
    }

    [Fact]
    public async Task SingleOrder_UnexistingInRepository_ResponseIsCorrect()
    {
        // Arrange
        var clock = new FakeClock(DateTime.UtcNow); 

        var repo = new InMemoryOrderRepository();
        
        repo.Populate(GetOldCompletedOrder(clock, 1));

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
        var response = await sut.CompleteOrdersAsync(new[] { 2 }, CancellationToken.None);

        //Assert
        Assert.False(response.OrdersSuccessfullyUpdated);
        Assert.Contains(2, response.UnsuccessfullyNotifiedOrders);
    }
    
    [Fact]
    public async Task SingleOrder_OldWithoutOrderLines_ResponseIsCorrect()
    {
        // Arrange
        var clock = new FakeClock(DateTime.UtcNow); 

        var repo = new InMemoryOrderRepository();
        
        repo.Populate(GetOldNoOrderLinesOrder(clock, 1));

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
        var response = await sut.CompleteOrdersAsync(new[] { 1 }, CancellationToken.None);

        //Assert
        Assert.False(response.OrdersSuccessfullyUpdated);
        Assert.Contains(1, response.FailedToUpdateOrders);
    }
    
    private Order GetOldCompletedOrder(ISystemClock clock, int orderId)
    {
        return new Order
        {
            Id = orderId,
            OrderState = OrderState.Submitted,
            OrderDate = clock.UtcNow.AddYears(-1),
            OrderLines = new[]
            {
                new OrderLine() { Id = 1, OrderedQuantity = 10, DeliveredQuantity = 10, ProductId = 3 },
                new OrderLine() { Id = 2, OrderedQuantity = 3, DeliveredQuantity = 3, ProductId = 5 }
            }
        };
    }
    
    private Order GetOldIncompletedOrder(ISystemClock clock, int orderId)
    {
        return new Order
        {
            Id = orderId,
            OrderState = OrderState.Submitted,
            OrderDate = clock.UtcNow.AddYears(-1),
            OrderLines = new[]
            {
                new OrderLine() { Id = 1, OrderedQuantity = 16, DeliveredQuantity = 10, ProductId = 3 },
                new OrderLine() { Id = 2, OrderedQuantity = 6, DeliveredQuantity = 3, ProductId = 5 }
            }
        };
    }
    
    private Order GetRecentCompletedOrder(ISystemClock clock, int orderId)
    {
        return new Order
        {
            Id = orderId,
            OrderState = OrderState.Submitted,
            OrderDate = clock.UtcNow.AddMonths(-2),
            OrderLines = new[]
            {
                new OrderLine() { Id = 1, OrderedQuantity = 10, DeliveredQuantity = 10, ProductId = 3 },
                new OrderLine() { Id = 2, OrderedQuantity = 3, DeliveredQuantity = 3, ProductId = 5 }
            }
        };
    }
    
    private Order GetOldNoOrderLinesOrder(ISystemClock clock, int orderId)
    {
        return new Order
        {
            Id = orderId,
            OrderState = OrderState.Submitted,
            OrderDate = clock.UtcNow.AddYears(-1)
        };
    }
}