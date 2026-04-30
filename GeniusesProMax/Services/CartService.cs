using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Infrastruccture.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GeniusesProMax.Services
{
    public class CartService : ICartService
    {
        private readonly AppDbContext _db;

        public CartService(AppDbContext db)
        {
            _db = db;
        }   


        public async Task AddToCartAsync(int userId, AddToCartRequest request)
        {
            var product = await _db.Products.FindAsync(request.ProductId);

            if (product is null)
            {
                throw new InvalidOperationException("Product not found.");
            }

            if (product.StockQuantity < request.Quantity)
            {
                throw new InvalidOperationException("Not enough stock available.");
            }

            var cart = await _db.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null)
            {
                cart = new Cart { UserId = userId };

                _db.Carts.Add(cart);

                await _db.SaveChangesAsync();   
            }

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);

            if (existingItem is not null)
            {
                var newQuantity = existingItem.Quantity + request.Quantity;

                if (newQuantity > product.StockQuantity) throw new InvalidOperationException("total quantity exceeds available stock.");

                existingItem.Quantity = newQuantity;
            }
            else
            {
                cart.Items.Add(new CartItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity
                });
            }

            await _db.SaveChangesAsync();
        }

        public async Task<CartDto> GetCartItemsAsync(int userId)
        {
            var cart = await _db.Carts
                .AsNoTracking()
                .Include(c => c.Items)
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart is null) return new CartDto();

            return new CartDto
            {
                Id = cart.Id,
                Items = cart.Items.Select(i => new CartItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    UnitPrice = i.Product.Price
                }).ToList()
            };
        }

        public async Task RemoveCartItemAsync(int userId, int cartItemId)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId); 

            if (cart is null) throw new InvalidOperationException("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (item is not null)
                cart.Items.Remove(item);


            await _db.SaveChangesAsync();
            
        }

        public async Task UpdateCartItemAsync(int userId, int cartItemId, UpdateCartItemRequest request)
        {
            var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);  

            if (cart is null) throw new InvalidOperationException("Cart not found.");

            var item = cart.Items.FirstOrDefault(i => i.Id == cartItemId);

            if (item is null) throw new InvalidOperationException("Cart item not found.");

            if (request.Quantity <= 0)
            {
                cart.Items.Remove(item);
            }else
            {
                var product = await _db.Products.FindAsync(item.ProductId);
                if (product is null || product.StockQuantity < request.Quantity)
                {
                    throw new InvalidOperationException("Not enough stock available.");
                }
                item.Quantity = request.Quantity;
            }

            await _db.SaveChangesAsync();
        }
    }
}
