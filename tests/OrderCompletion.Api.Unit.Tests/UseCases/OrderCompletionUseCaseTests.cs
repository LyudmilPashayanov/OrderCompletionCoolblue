using Microsoft.Extensions.Logging;
using Moq;
using OrderCompletion.Api.Models;
using OrderCompletion.Api.OrderUseCaseRequirements;
using OrderCompletion.Api.Ports;
using OrderCompletion.Api.Utilities;
using Polly;
using Xunit;

namespace OrderCompletion.Api.Unit.Tests.UseCases;

public class OrderCompletionUseCaseTests
{
    private static readonly DateTime FixedNow = new DateTime(2025, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    private readonly IAsyncPolicy _noOpPolicy = Policy.NoOpAsync(); // no retries in unit tests TODO: maybe we need to retry in unit tests?
    private readonly Mock<ILogger<OrderCompletionUseCase>> _logger = new(); 

    private readonly Mock<IOrderCompletionRepository> _orderCompletionRepository = new();
    private readonly Mock<INotificationClient> _notificationClientMock = new();
    private readonly Mock<ISystemClock> _clockMock = new();
    
    private IOrderCompletionUseCase CreateSut(params IOrderRequirement[] requirements)
    {
        return new OrderCompletionUseCase(
            _orderCompletionRepository.Object,
            _notificationClientMock.Object,
            requirements,
            _logger.Object);
    }

    [Fact]
    public async Task GivenRecentOrder_CompleteOrders_OrderIsNotCompleted()
    {
        //Arrange
        const int orderId = 1;
        SetupRecentOrder(orderId);
        
        var reqDelivered = new FullyDeliveredRequirements();
        var reqAge = new OrderAgeRequirement(_clockMock.Object);
        IOrderCompletionUseCase sut = CreateSut(reqDelivered, reqAge);
        
        _clockMock.Setup(c => c.UtcNow).Returns(FixedNow);
        
        //Act
        await sut.CompleteOrdersAsync(new [] {orderId}, CancellationToken.None);
        
        // Assert
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()), Times.Never);
        _orderCompletionRepository.Verify(x =>
                x.CompleteOrdersAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(orderId)), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GivenOrderIsNotFullyDelivered_CompleteOrders_OrderIsNotCompleted()
    {
        const int orderId = 2;

        SetupIncompleteOrder(orderId);
        
        var reqDelivered = new FullyDeliveredRequirements();
        var reqAge = new OrderAgeRequirement(_clockMock.Object);
        IOrderCompletionUseCase sut = CreateSut(reqDelivered, reqAge);
        
        _clockMock.Setup(c => c.UtcNow).Returns(FixedNow);
        
        await sut.CompleteOrdersAsync([orderId], CancellationToken.None);

        _orderCompletionRepository.Verify(x => x.CompleteOrdersAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(orderId)), It.IsAny<CancellationToken>()), Times.Never);
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenOldOrderIsFullyDelivered_CompleteOrders_OrderCompleted()
    {
        const int orderId = 3;
        SetupOldCompletedOrder(orderId);
        
        _clockMock.Setup(c => c.UtcNow).Returns(FixedNow);

        _notificationClientMock
            .Setup(n => n.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var reqDelivered = new FullyDeliveredRequirements();
        var reqAge = new OrderAgeRequirement(_clockMock.Object);
        IOrderCompletionUseCase sut = CreateSut(reqDelivered, reqAge);
        
        
        await sut.CompleteOrdersAsync([orderId], CancellationToken.None);

        _orderCompletionRepository.Verify(x => x.CompleteOrdersAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(orderId)), It.IsAny<CancellationToken>()), Times.Once);
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenMultipleOrders_CompleteOrders_OnlyOldFullyDeliveredOrdersAreCompleted()
    {
        // Arrange
        Order order1 = SetupOldCompletedOrder(1);
        Order order2 = SetupRecentOrder(2);
        Order order3 = SetupOldCompletedOrder(3);
        Order order4 = SetupIncompleteOrder(4);
        
        // Setup repository mock to return matching orders for any collection of IDs
        _orderCompletionRepository
            .Setup(x => x.GetOrdersByIdAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<int> ids, CancellationToken _) =>
            {
                var orders = new List<Order>();
                if (ids.Contains(1)) orders.Add(order1);
                if (ids.Contains(2)) orders.Add(order2);
                if (ids.Contains(3)) orders.Add(order3);
                if (ids.Contains(4)) orders.Add(order4);
                return orders;
            });
        
        // Notification returns true for orders 1 and 3
        _notificationClientMock
            .Setup(n => n.NotifyOrderCompletedAsync(It.Is<int>(i => i == 1 || i == 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // For other orders, ensure the mock returns false if called (not expected)
        _notificationClientMock
            .Setup(n => n.NotifyOrderCompletedAsync(It.Is<int>(i => i != 1 && i != 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var reqDelivered = new FullyDeliveredRequirements();
        var reqAge = new OrderAgeRequirement(_clockMock.Object);
        IOrderCompletionUseCase sut = CreateSut(reqDelivered, reqAge);

        _clockMock.Setup(c => c.UtcNow).Returns(FixedNow);

        // Act
        await sut.CompleteOrdersAsync(new[] { 1, 2, 3, 4 }, CancellationToken.None);

        // Assert
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(3, It.IsAny<CancellationToken>()), Times.Once);
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(It.Is<int>(i => i == 2 || i == 4), It.IsAny<CancellationToken>()), Times.Never);

        _orderCompletionRepository.Verify(x =>
                x.CompleteOrdersAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 2 && ids.Contains(1) && ids.Contains(3)), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GivenNotificationFails_OldCompletedOrder_OrderIsNotMarkedCompleted()
    {
        // Arrange
        const int orderId = 4;
        SetupOldCompletedOrder(orderId);

        // Notification fails
        _notificationClientMock
            .Setup(n => n.NotifyOrderCompletedAsync(It.Is<int>(i => i == orderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clockMock.Setup(c => c.UtcNow).Returns(FixedNow);

        var sut = CreateSut(new FullyDeliveredRequirements(), new OrderAgeRequirement(_clockMock.Object));

        // Act
        await sut.CompleteOrdersAsync(new[] { orderId }, CancellationToken.None);

        // Assert
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(It.Is<int>(i => i == orderId), It.IsAny<CancellationToken>()), Times.Once);
        _orderCompletionRepository.Verify(x =>
                x.CompleteOrdersAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Contains(orderId)), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    
    private Order SetupRecentOrder(int orderId)
    {
        var order = new Order
        {
            Id = orderId,
            OrderDate = FixedNow.AddDays(-1),
            OrderLines = new List<OrderLine>
            {
                new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 }
            }
        };
        
        _orderCompletionRepository
            .Setup(x => x.GetOrdersByIdAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1 && ids.Contains(orderId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });
        
        return order;
    }

    private Order SetupIncompleteOrder(int orderId)
    {
        var order = new Order
        {
            Id = orderId,
            OrderState = OrderState.Submitted,
            OrderDate = FixedNow.AddYears(-1),
            OrderLines = new List<OrderLine>
            {
                new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 },
                new OrderLine { ProductId = 2, OrderedQuantity = 10, DeliveredQuantity = 0 } // not delivered
            }
        };

        _orderCompletionRepository
            .Setup(x => x.GetOrdersByIdAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1 && ids.Contains(orderId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });
        
        return order;
    }

    private Order SetupOldCompletedOrder(int orderId)
    {
        var order = new Order
        {
            Id = orderId,
            OrderState = OrderState.Submitted,
            OrderDate = FixedNow.AddYears(-1),
            OrderLines = new List<OrderLine>
            {
                new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 },
                new OrderLine { ProductId = 2, OrderedQuantity = 2, DeliveredQuantity = 2 }
            }
        };

        _orderCompletionRepository
            .Setup(x => x.GetOrdersByIdAsync(It.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1 && ids.Contains(orderId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { order });
        
        return order;
    }
}