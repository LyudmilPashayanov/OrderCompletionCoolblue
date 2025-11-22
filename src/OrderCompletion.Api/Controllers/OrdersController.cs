using Microsoft.AspNetCore.Mvc;
using OrderCompletion.Api.Ports;

namespace OrderCompletion.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderCompletionUseCase _orderCompleteUsecase;

    public OrdersController(IOrderCompletionUseCase orderCompleteUsecase)
    {
        _orderCompleteUsecase = orderCompleteUsecase;
    }

    [HttpPost(Name = "Complete")]
    public async Task<IActionResult> Complete(List<int> orderIds, CancellationToken ct)
    {
        CompleteOrdersResponse response = await _orderCompleteUsecase.CompleteOrdersAsync(orderIds, ct);
        
        if (response.OrdersSuccessfullyUpdated == false || 
            response.SuccessfullyNotifiedOrders.Count == 0)
        {
            return BadRequest(new
            {
                Message = "None of the requested orders were updated successfully.",
                response.SuccessfullyNotifiedOrders,
                response.UnsuccessfullyNotifiedOrders,
                response.FailedToUpdateOrders
            });
        }
        
        return Ok(response);
    }
}