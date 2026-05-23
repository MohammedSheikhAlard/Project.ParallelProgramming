using GeniusesProMax.DTOs;

namespace GeniusesProMax.Interfaces
{
    public interface IOrderService
    {
        Task<OrderDto> CheckoutAsync(int userId);
        Task<List<OrderDto>> GetOrderHistoryAsync(int userId);
        Task<OrderDto> CheckoutWithPessimisticLockAsync(int userId);
    }
}
