namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter.Dtos;

public class OrderDto
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string OrderStateId { get; set; }
    public List<OrderLineDto> OrderLines { get; set; }
}