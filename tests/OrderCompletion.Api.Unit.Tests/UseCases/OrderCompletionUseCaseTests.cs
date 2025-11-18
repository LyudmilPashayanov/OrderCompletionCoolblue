using Moq;
using OrderCompletion.Api.Models;
using OrderCompletion.Api.Ports;
using Xunit;

namespace OrderCompletion.Api.Unit.Tests.UseCases;

public class OrderCompletionUseCaseTests
{
    private static readonly DateTime Yesterday = DateTime.UtcNow.AddDays(-1);
    private static readonly DateTime Yesteryear = DateTime.UtcNow.AddYears(-1);

    private readonly Mock<IOrderCompletionRepository> _orderCompletionRepository = new();
    private readonly Mock<INotificationClient> _notificationClientMock = new();
    private readonly IOrderCompletionUseCase _sut;

    public OrderCompletionUseCaseTests()
    {
        _sut = new OrderCompletionUseCase(_orderCompletionRepository.Object, _notificationClientMock.Object);
    }

    [Fact]
    public void GivenRecentOrder_CompleteOrders_OrderIsNotCompleted()
    {
        const int orderId = 1;

        SetupRecentOrder(orderId);

        _sut.CompleteOrders([orderId]);

        _orderCompletionRepository.Verify(x => x.CompleteOrder(orderId), Times.Never);
        _notificationClientMock.Verify(x => x.OrderCompleted(orderId), Times.Never);
    }

    [Fact]
    public void GivenOrderIsNotFullyDelivered_CompleteOrders_OrderIsNotCompleted()
    {
        const int orderId = 2;

        SetupIncompleteOrder(orderId);

        _sut.CompleteOrders([orderId]);

        _orderCompletionRepository.Verify(x => x.CompleteOrder(orderId), Times.Never);
        _notificationClientMock.Verify(x => x.OrderCompleted(orderId), Times.Never);
    }

    [Fact]
    public void GivenOldOrderIsFullyDelivered_CompleteOrders_OrderCompleted()
    {
        const int orderId = 3;

        SetupOldCompletedOrder(orderId);

        _sut.CompleteOrders([orderId]);

        _orderCompletionRepository.Verify(x => x.CompleteOrder(orderId), Times.Once);
        _notificationClientMock.Verify(x => x.OrderCompleted(orderId), Times.Once);
    }

    [Fact]
    public void GivenMultipleOrders_CompleteOrders_OnlyOldFullyDeliveredOrdersAreCompleted()
    {
        SetupOldCompletedOrder(1);
        SetupRecentOrder(2);
        SetupOldCompletedOrder(3);
        SetupIncompleteOrder(4);

        _sut.CompleteOrders([1, 2, 3, 4]);

        _orderCompletionRepository.Verify(x => x.CompleteOrder(It.IsIn(1, 3)), Times.Exactly(2));
        _orderCompletionRepository.Verify(x => x.CompleteOrder(It.IsNotIn(1, 3)), Times.Never);
        _notificationClientMock.Verify(x => x.OrderCompleted(It.IsIn(1, 3)), Times.Exactly(2));
        _notificationClientMock.Verify(x => x.OrderCompleted(It.IsNotIn(1, 3)), Times.Never);
    }

    private void SetupRecentOrder(int orderId)
    {
        _orderCompletionRepository
            .Setup(x => x.GetOrderById(orderId))
            .Returns(new Order
            {
                Id = orderId,
                OrderDate = Yesterday,
                OrderLines =
                [
                    new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 }
                ]
            });
    }

    private void SetupIncompleteOrder(int orderId)
    {
        _orderCompletionRepository
            .Setup(x => x.GetOrderById(orderId))
            .Returns(new Order
            {
                Id = orderId,
                OrderState = OrderState.Submitted,
                OrderDate = Yesteryear,
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
            .Setup(x => x.GetOrderById(orderId))
            .Returns(new Order
            {
                Id = orderId,
                OrderState = OrderState.Submitted,
                OrderDate = Yesteryear,
                OrderLines =
                [
                    new OrderLine { ProductId = 1, OrderedQuantity = 10, DeliveredQuantity = 10 },
                    new OrderLine { ProductId = 2, OrderedQuantity = 2, DeliveredQuantity = 2}
                ]
            });
    }
}