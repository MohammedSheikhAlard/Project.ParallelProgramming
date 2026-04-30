using GeniusesProMax.DTOs;

namespace GeniusesProMax.Interfaces
{
    public interface ICartService
    {
        public Task AddToCartAsync(int userId, AddToCartRequest request);
        public Task<CartDto> GetCartItemsAsync(int userId);
        public Task UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemRequest request);
        public Task RemoveCartItemAsync(int userId, int cartItemId);
    }
}
