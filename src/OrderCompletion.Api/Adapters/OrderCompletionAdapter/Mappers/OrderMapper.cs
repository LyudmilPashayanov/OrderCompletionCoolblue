using OrderCompletion.Api.Adapters.OrderCompletionAdapter.Dtos;
using OrderCompletion.Api.Models;

namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter.Mappers;

public static class OrderMapper
{
    public static Order ToDomain(this OrderDto orderDto) => new Order
    {
        Id = orderDto.Id,
        OrderDate = orderDto.OrderDate,
        OrderState = Enum.Parse<OrderState>(orderDto.OrderStateId),
        OrderLines = orderDto.OrderLines.Select(ol => ol.ToDomain()).ToList()
    };

    private static OrderLine ToDomain(this OrderLineDto orderLineDto) => new OrderLine
    {
        Id = orderLineDto.Id,
        ProductId = orderLineDto.ProductId,
        OrderedQuantity = orderLineDto.OrderedQuantity,
        DeliveredQuantity = orderLineDto.DeliveredQuantity
    };
}