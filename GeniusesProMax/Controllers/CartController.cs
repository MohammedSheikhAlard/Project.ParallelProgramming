using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GeniusesProMax.Controllers
{
    [Route("api/Cart")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        private int GetUserId() =>
       int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpPost("add-to-cart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
               
                await _cartService.AddToCartAsync(GetUserId(), request);

                return Ok(new { Message = "Product added to cart successfully." });

            }
            catch (InvalidOperationException ex )
            {

                return BadRequest(ex.Message);
            }
        }

        [HttpGet("get-cart-items")]
        public async Task<IActionResult> GetCartItems()
        {
            var cart = await _cartService.GetCartItemsAsync(GetUserId());

            if(cart == null || cart.Items.Count == 0) return NotFound("Cart is empty.");

            return Ok(cart);
        }

        [HttpPut("update-items/{cartItemId}")]
        public async Task<IActionResult> UpdateItem(int cartItemId, UpdateCartItemRequest request)
        {
            try
            {
                await _cartService.UpdateCartItemAsync(GetUserId(), cartItemId, request);

                return Ok(new {Message = "Cart item Updated."});
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("delete-item/{cartItemId}")]
        public async Task<IActionResult> DeleteItem(int cartItemId)
        {
            try
            {
                await _cartService.RemoveCartItemAsync(GetUserId(), cartItemId);

                return Ok(new { Message = "Cart item removed." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    }
}
