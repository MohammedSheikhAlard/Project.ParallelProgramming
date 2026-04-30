using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GeniusesProMax.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : ControllerBase
    {
        public readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId() =>
       int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("checkout")]
        public async Task<ActionResult<OrderDto>> Checkout()
        {
            try
            {
                var order = await _orderService.CheckoutAsync(GetUserId());
                return Ok(order);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("get-order-history")]
        public async Task<ActionResult<List<OrderDto>>> GetHistory()
        {
            var orders = await _orderService.GetOrderHistoryAsync(GetUserId());
            return Ok(orders);
        }
    }
}
