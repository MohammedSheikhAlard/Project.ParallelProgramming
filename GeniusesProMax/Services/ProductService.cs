using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Infrastructure.Caching;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GeniusesProMax.Services
{
    public class ProductService : IProductService
    {

        private readonly AppDbContext _db;
        private readonly ICacheService _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
        public ProductService(AppDbContext db,ICacheService cache)
        {
            _db = db;
            _cache = cache; 
        }


        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {

            string cacheKey = $"products:{id}";

            var cached = await _cache.GetAsync<ProductDto>(cacheKey);
            if(cached != null)
                return cached;


            var product = await _db.Products.AsNoTracking()
                .Where(p => p.Id == id)
                .Select(p => new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    StockQuantity = p.StockQuantity,
                }).FirstOrDefaultAsync();

            await _cache.SetAsync(cacheKey, product, CacheDuration); 

            return product;

        }

        public async Task<PagedResult<ProductDto>> GetProductsAsync(int pageNumber, int pageSize)
        {
            // for requirment 6 (caching)

            // Generate a cache key based on the page number and size
            string cachekey = $"products:page:{pageNumber}:size:{pageSize}";

            // check if the data is already cached & returned if available
            var cached = await _cache.GetAsync<PagedResult<ProductDto>>(cachekey);
            if (cached != null) 
                return cached;


            // get the data from the database if not cached
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


            var result =  new PagedResult<ProductDto>
                            {
                                Items = items,
                                TotalCount = totalCount,
                                PageNumber = pageNumber,
                                PageSize = pageSize 
                            };

            await _cache.SetAsync(cachekey, result, CacheDuration); // cache the result for future requests

            return result;
        }
    }
}
