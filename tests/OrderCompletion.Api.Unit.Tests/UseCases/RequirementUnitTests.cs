using Moq;
using OrderCompletion.Api.Models;
using OrderCompletion.Api.OrderUseCaseRequirements;
using OrderCompletion.Api.Utilities;
using Xunit;

namespace OrderCompletion.Api.Unit.Tests.UseCases;

public class RequirementUnitTests
{
    private static readonly DateTime FixedNow = new DateTime(2025, 11, 20, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void FullyDeliveredRequirements_EmptyOrderLines_ReturnsFalse()
    {
        var req = new FullyDeliveredRequirements();

        var order = new Order
        {
            Id = 1,
            OrderLines = Array.Empty<OrderLine>()
        };

        Assert.False(req.Fulfils(order));
    }

    [Fact]
    public void FullyDeliveredRequirements_AllLinesDelivered_ReturnsTrue()
    {
        var req = new FullyDeliveredRequirements();

        var order = new Order
        {
            Id = 2,
            OrderLines = new[]
            {
                new OrderLine { ProductId = 1, OrderedQuantity = 2, DeliveredQuantity = 2 },
                new OrderLine { ProductId = 2, OrderedQuantity = 5, DeliveredQuantity = 5 }
            }
        };

        Assert.True(req.Fulfils(order));
    }

    [Fact]
    public void FullyDeliveredRequirements_SomeLineNotDelivered_ReturnsFalse()
    {
        var req = new FullyDeliveredRequirements();

        var order = new Order
        {
            Id = 3,
            OrderLines = new[]
            {
                new OrderLine { ProductId = 1, OrderedQuantity = 2, DeliveredQuantity = 2 },
                new OrderLine { ProductId = 2, OrderedQuantity = 5, DeliveredQuantity = null }
            }
        };

        Assert.False(req.Fulfils(order));
    }

    [Fact]
    public void OrderAgeRequirement_BoundaryExactlySixMonths_ReturnsTrue()
    {
        var clock = new Mock<ISystemClock>();
        // Now = FixedNow
        clock.Setup(c => c.UtcNow).Returns(FixedNow);

        var req = new OrderAgeRequirement(clock.Object);

        // Exactly six months ago -> should be considered "at least 6 months old"
        var order = new Order { OrderDate = FixedNow.AddMonths(-6) };

        Assert.True(req.Fulfils(order));
    }

    [Fact]
    public void OrderAgeRequirement_JustBeforeSixMonths_ReturnsFalse()
    {
        var clock = new Mock<ISystemClock>();
        clock.Setup(c => c.UtcNow).Returns(FixedNow);

        var req = new OrderAgeRequirement(clock.Object);

        // 6 months minus 1 second -> not old enough
        var order = new Order { OrderDate = FixedNow.AddMonths(-6).AddSeconds(1) };

        Assert.False(req.Fulfils(order));
    }

    [Fact]
    public void OrderAgeRequirement_WellOlderThanSixMonths_ReturnsTrue()
    {
        var clock = new Mock<ISystemClock>();
        clock.Setup(c => c.UtcNow).Returns(FixedNow);

        var req = new OrderAgeRequirement(clock.Object);

        var order = new Order { OrderDate = FixedNow.AddYears(-1) };

        Assert.True(req.Fulfils(order));
    }
}