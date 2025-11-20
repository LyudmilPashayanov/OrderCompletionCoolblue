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

    private readonly Mock<IOrderCompletionRepository> _orderCompletionRepository = new();
    private readonly Mock<INotificationClient> _notificationClientMock = new();
    private readonly Mock<ISystemClock> _clockMock = new();
    
    private IOrderCompletionUseCase CreateSut(params IOrderRequirement[] requirements)
    {
        return new OrderCompletionUseCase(
            _orderCompletionRepository.Object,
            _notificationClientMock.Object,
            requirements,
            _noOpPolicy);
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
        _orderCompletionRepository.Verify(x => x.CompleteOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Never);
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()), Times.Never);
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

        _orderCompletionRepository.Verify(x => x.CompleteOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Never);
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

        _orderCompletionRepository.Verify(x => x.CompleteOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GivenMultipleOrders_CompleteOrders_OnlyOldFullyDeliveredOrdersAreCompleted()
    {
        SetupOldCompletedOrder(1);
        SetupRecentOrder(2);
        SetupOldCompletedOrder(3);
        SetupIncompleteOrder(4);
        
        _notificationClientMock
            .Setup(n => n.NotifyOrderCompletedAsync(It.IsIn(1, 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        var reqDelivered = new FullyDeliveredRequirements();
        var reqAge = new OrderAgeRequirement(_clockMock.Object);
        IOrderCompletionUseCase sut = CreateSut(reqDelivered, reqAge);
        
        _clockMock.Setup(c => c.UtcNow).Returns(FixedNow);

        await sut.CompleteOrdersAsync([1, 2, 3, 4], CancellationToken.None);

        _orderCompletionRepository.Verify(x => x.CompleteOrderAsync(It.IsIn(1, 3), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _orderCompletionRepository.Verify(x => x.CompleteOrderAsync(It.IsNotIn(1, 3), It.IsAny<CancellationToken>()), Times.Never);
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(It.IsIn(1, 3), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(It.IsNotIn(1, 3), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GivenNotificationFails_OrderIsNotMarkedCompleted()
    {
        const int orderId = 4;
        SetupOldCompletedOrder(orderId);

        // Notification fails (returns false)
        _notificationClientMock
            .Setup(n => n.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _clockMock.Setup(c => c.UtcNow).Returns(FixedNow);

        var sut = CreateSut(new FullyDeliveredRequirements(), new OrderAgeRequirement(_clockMock.Object));

        await sut.CompleteOrdersAsync(new[] { orderId }, CancellationToken.None);

        _notificationClientMock.Verify(x => x.NotifyOrderCompletedAsync(orderId, It.IsAny<CancellationToken>()), Times.Once);
        _orderCompletionRepository.Verify(x => x.CompleteOrderAsync(orderId, It.IsAny<CancellationToken>()), Times.Never);
    }
    
    private void SetupRecentOrder(int orderId)
    {
        _orderCompletionRepository
            .Setup(x => x.GetOrderByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                OrderDate = FixedNow.AddDays(-1),
                OrderLines =
                [
                    new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 }
                ]
            });
    }

    private void SetupIncompleteOrder(int orderId)
    {
        _orderCompletionRepository
            .Setup(x => x.GetOrderByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                OrderState = OrderState.Submitted,
                OrderDate = FixedNow.AddYears(-1),
                OrderLines =
                [
                    new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 },
                    new OrderLine { ProductId = 2, OrderedQuantity = 10, }
                ]
            });
    }

    private void SetupOldCompletedOrder(int orderId)
    {
        _orderCompletionRepository
            .Setup(x => x.GetOrderByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Order
            {
                Id = orderId,
                OrderState = OrderState.Submitted,
                OrderDate = FixedNow.AddYears(-1),
                OrderLines =
                [
                    new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 },
                    new OrderLine { ProductId = 2, OrderedQuantity = 2, DeliveredQuantity = 2 }
                ]
            });
    }
}