using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Infrastruccture.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GeniusesProMax.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;

        public OrderService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<OrderDto> CheckoutAsync(int userId)
        {
            await using var transaction = await _db.Database.BeginTransactionAsync();

            try
            {
                var cart = await _db.Carts.Include(c => c.Items)
                                            .ThenInclude(i => i.Product)
                                            .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.Items.Any())
                    throw new InvalidOperationException("Cart is Empty");

                foreach (var item in cart.Items)
                {
                    var product = item.Product;

                    if (product.StockQuantity < item.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for {product.Name}");

                    product.StockQuantity -= item.Quantity;

                }

                // Crate Order 
                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pending,
                    Items = cart.Items.Select(i => new OrderItem
                    {
                        ProductId = i.ProductId,
                        Quantity = i.Quantity,
                        UnitPrice = i.Product.Price

                    }).ToList()
                };

                order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);

                _db.Orders.Add(order);

                // Clear the Cart 
                _db.CartItems.RemoveRange(cart.Items);

                await _db.SaveChangesAsync();



                // Simulate Payment
                order.Status = OrderStatus.Paid;
                
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return new OrderDto
                {
                    Id = order.Id,
                    OrderDate = order.OrderDate,
                    Status = order.Status.ToString(),
                    TotalAmount = order.TotalAmount,
                    Items = order.Items.Select(i => new OrderItemDto
                    {
                        ProductName = i.Product.Name,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice
                    }).ToList()
                };
            }
            catch 
            {

                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<OrderDto>> GetOrderHistoryAsync(int userId)
        {
            return await _db.Orders.Where(o => o.UserId == userId)
                                    .AsNoTracking()
                                    .OrderByDescending(o => o.OrderDate)
                                    .Select(o => new OrderDto
                                    {
                                        Id = o.Id,
                                        OrderDate = o.OrderDate,
                                        Status = o.Status.ToString(),
                                        TotalAmount = o.TotalAmount,
                                        Items = o.Items.Select(i => new OrderItemDto
                                        {
                                            ProductName = i.Product.Name,
                                            Quantity = i.Quantity,
                                            UnitPrice = i.UnitPrice
                                        }).ToList()
                                    }).ToListAsync();
        }
    }
}
