using GeniusesProMax.DTOs;
using GeniusesProMax.Interfaces;
using Infrastruccture.Entities;
using Infrastructure.BackgroundJob;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GeniusesProMax.Services
{
    public class OrderService : IOrderService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private static readonly SemaphoreSlim _checkoutSemaphore;
        private readonly IBackgroundTaskQueue _taskQueue;

        private static SemaphoreSlim? _semaphore;
        private static readonly object _semaphoreLock = new();

        public OrderService(AppDbContext db,IConfiguration config,IBackgroundTaskQueue taskQueue)
        {
            _db = db;
            _config = config;
            _taskQueue = taskQueue;

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

            // this is the solution for the second problem (Semaphore Timeout)

            int timeoutSeconds = _config.GetValue<int>("Checkout:SemaphoreTimeoutSeconds", 30);
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);


            if (!await _semaphore!.WaitAsync(timeout))
            {
                throw new InvalidOperationException(
                    "System is under heavy load. Please try again later.");
            }




            // this is the solution for the first problem 

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
                    

                    // here will check the rowversion
                    await _db.SaveChangesAsync();



                    // Simulate Payment
                    order.Status = OrderStatus.Paid;

                    await _db.SaveChangesAsync();

                    await transaction.CommitAsync();


                    // this is for the third problem (Background Tasks)

                    // Enqueue background tasks (fire-and-forget)
                    await _taskQueue.QueueBackgroundWorkItemAsync(async (cancellationToken) =>
                    {
                        // Simulate invoice generation (a short delay)
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                        // In a real app: generate PDF, send email, etc.
                        // Log success (ILogger can be injected via a static or captured via provider)
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
                catch(DbUpdateConcurrencyException) when(attempt < maxRetries -1)
                {
                    attempt++;

                    await Task.Delay(50);

                    _db.ChangeTracker.Clear();
                }
                catch(DbUpdateConcurrencyException)
                {
                    throw new InvalidOperationException("Checkout failed due to concurrent updates. Please try again.");
                }
                finally // 2
                {
                    // Always release the semaphore, even if an exception occurred.
                    _semaphore!.Release();
                }
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
