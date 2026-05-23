using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Infrastruccture.Entities;
using Infrastructure.BackgroundJob;
using Infrastructure.Caching;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GeniusesProMax.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IBackgroundTaskQueue _taskQueue;
        private readonly ICacheService _cache;

        private static SemaphoreSlim? _semaphore;
        private static readonly object _semaphoreLock = new();

        public OrderService(AppDbContext db,IConfiguration config,IBackgroundTaskQueue taskQueue,ICacheService cache)
        {
            _db = db;
            _config = config;
            _taskQueue = taskQueue; 
            _cache = cache;

            if (_semaphore == null)
            {
                lock (_semaphoreLock)
                {
                    if (_semaphore == null)
                    {
                        int maxConcurrentCheckouts = _config.GetValue<int>("MaxConcurrentCheckouts", 10);

                        _semaphore = new SemaphoreSlim(maxConcurrentCheckouts, maxConcurrentCheckouts);
                    }
                }
            }
        }

        public async Task<OrderDto> CheckoutAsync(int userId)
        {
            // ================================================================
            // this is the solution for the second problem (Semaphore Timeout)
            // ================================================================
            int timeoutSeconds = _config.GetValue<int>("Checkout:SemaphoreTimeoutSeconds", 30);
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            // Acquire the semaphore – this request now owns one slot.
            // If all slots are busy, we wait up to 'timeout' seconds.
            if (!await _semaphore!.WaitAsync(timeout))
            {
                throw new InvalidOperationException(
                    "System is under heavy load. Please try again later.");
            }

            try
            {
                // ============================================================
                // this is the solution for the first problem (Concurrency)
                // ============================================================
                int maxRetries = 3;
                int attempt = 0;

                while (true)
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

                        // Create Order
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

                        // here will check the rowversion
                        await _db.SaveChangesAsync();

                        // Simulate Payment
                        order.Status = OrderStatus.Paid;
                        await _db.SaveChangesAsync();

                        await transaction.CommitAsync();

                        // =========================================================
                        // this is for requirement 6 (caching) – Invalidate cache
                        // =========================================================
                        foreach (var item in cart.Items)
                        {
                            await _cache.RemoveAsync($"Product_{item.ProductId}");
                        }
                        for (int page = 1; page <= 5; page++)
                        {
                            await _cache.RemoveAsync($"products:page:{page}:size:10");
                        }

                        // =======================================================
                        // this is for the third problem (Background Tasks)
                        // =======================================================
                        await _taskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
                        {
                            // Simulate invoice generation (a short delay)
                            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                            Console.WriteLine($"Invoice generated for order {order.Id}.");
                        });

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
                    catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
                    {
                        attempt++;
                        await Task.Delay(50);   // small backoff
                        _db.ChangeTracker.Clear();
                        // loop continues – semaphore is STILL HELD (correct)
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        throw new InvalidOperationException(
                            "Checkout failed due to concurrent updates. Please try again.");
                    }
                }
            }
            finally
            {
                // Release the semaphore exactly once – after success or final failure
                _semaphore!.Release();
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

        public async Task<OrderDto> CheckoutWithPessimisticLockAsync(int userId)
        {
            // Semaphore (same as optimistic)
            int timeoutSeconds = _config.GetValue<int>("Checkout:SemaphoreTimeoutSeconds", 30);
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            if (!await _semaphore!.WaitAsync(timeout))
                throw new InvalidOperationException("System is under heavy load. Please try again later.");

            try
            {
                await using var transaction = await _db.Database.BeginTransactionAsync();

                var cart = await _db.Carts
                    .Include(c => c.Items)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart is null || !cart.Items.Any())
                    throw new InvalidOperationException("Cart is Empty");

                var productIds = cart.Items.Select(i => i.ProductId).ToList();

                // Pessimistic lock
                var lockedProducts = await _db.Products
                    .FromSqlRaw("SELECT * FROM Products WITH (UPDLOCK) WHERE Id IN ({0})",
                        string.Join(",", productIds))
                    .ToListAsync();

                if (lockedProducts.Count != productIds.Count)
                    throw new InvalidOperationException("One or more products not found.");

                foreach (var item in cart.Items)
                {
                    var product = lockedProducts.First(p => p.Id == item.ProductId);
                    if (product.StockQuantity < item.Quantity)
                        throw new InvalidOperationException($"Insufficient stock for {product.Name}");

                    product.StockQuantity -= item.Quantity;
                }

                var order = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
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

                _db.CartItems.RemoveRange(cart.Items);

                await _db.SaveChangesAsync();

                order.Status = OrderStatus.Paid;
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                // Cache invalidation
                foreach (var item in cart.Items)
                {
                    await _cache.RemoveAsync($"products:{item.ProductId}");
                }
                for (int page = 1; page <= 5; page++)
                {
                    await _cache.RemoveAsync($"products:page:{page}:size:10");
                }

                // Background task
                await _taskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                    Console.WriteLine($"Invoice generated for order {order.Id}.");
                });

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
                throw; // transaction rolls back automatically
            }
            finally
            {
                _semaphore!.Release();
            }
        }
    }
}
