using Microsoft.AspNetCore.Mvc;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderCompletionUseCase _orderCompleteUsecase;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderCompletionUseCase orderCompleteUsecase, ILogger<OrdersController> logger)
    {
        _orderCompleteUsecase = orderCompleteUsecase;
        _logger = logger;
    }

    [HttpPost(Name = "Complete")]
    public async Task<IActionResult> Complete(List<int> orderIds, CancellationToken ct)
    {
        await _orderCompleteUsecase.CompleteOrdersAsync(orderIds, ct);

        return Ok();
    }
}