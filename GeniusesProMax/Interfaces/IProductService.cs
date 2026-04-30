using GeniusesProMax.DTOs;

namespace GeniusesProMax.Interfaces
{
    public interface IProductService
    {
        public Task<PagedResult<ProductDto>> GetProductsAsync(int pageNumber, int pageSize);

        public Task<ProductDto?> GetProductByIdAsync(int id);
    }
}
