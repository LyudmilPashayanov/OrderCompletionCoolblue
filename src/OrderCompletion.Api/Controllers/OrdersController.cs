using Microsoft.AspNetCore.Mvc;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderCompletionUseCase _usecase;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(IOrderCompletionUseCase usecase, ILogger<OrdersController> logger)
    {
        _usecase = usecase;
        _logger = logger;
    }

    [HttpPost(Name = "Complete")]
    public ActionResult Complete(List<int> orderIds)
    {
        _usecase.CompleteOrders(orderIds);

        return Ok();
    }
}