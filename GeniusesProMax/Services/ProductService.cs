using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GeniusesProMax.Services
{
    public class ProductService : IProductService
    {

        private readonly AppDbContext _db;

        public ProductService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _db.Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                }).FirstOrDefaultAsync();

            return product;

        }

        public async Task<PagedResult<ProductDto>> GetProductsAsync(int pageNumber, int pageSize)
        {
            var query = _db.Products.AsNoTracking(); 

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity
                })
                .ToListAsync(); 


            return new PagedResult<ProductDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize 
            };
        }
    }
}
