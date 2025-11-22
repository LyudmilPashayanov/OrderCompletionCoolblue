namespace OrderCompletion.Api.Adapters.OrderCompletionAdapter.Dtos;

public class OrderLineDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public int OrderedQuantity { get; set; }
    public int? DeliveredQuantity { get; set; }
}