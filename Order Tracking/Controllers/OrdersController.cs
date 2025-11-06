using Microsoft.EntityFrameworkCore;

namespace Order_Tracking.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController(IOrderService _orderService) : ControllerBase
    {
        [HttpGet("")]
        public async Task<IActionResult> Add([FromBody] OrderRequest request)
        {
            var isAdded = await _orderService.AddAsync(request);
            if (isAdded)
            {
                return Ok();
            }
            return BadRequest();
        }
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus([FromRoute]int id, [FromBody] UpdateOrderStatusRequest request)
        {
            var isUpdated = await _orderService.UpdateStatusAsync(id, request); 
            if (isUpdated)
            {
                return NoContent();
            }
            return BadRequest();
        }
    }
}