using Infrastruccture.Entities;
using Infrastructure.Data;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.BackgroundJob
{
    public class DailySalesBatchService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DailySalesBatchService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var cutoff = DateTime.UtcNow.AddDays(-1);

            int batchSize = 100;

            using var scope = _scopeFactory.CreateScope();  
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();


            int totalOrders = 0;
            decimal totalSales = 0;
            int page = 0;


            while (!stoppingToken.IsCancellationRequested)
            {

                var orders = await db.Orders
                            .AsNoTracking()
                            .Where(o => o.OrderDate >= cutoff && o.Status == OrderStatus.Paid)
                            .OrderBy(o => o.Id)
                            .Skip(page * batchSize)
                            .Take(batchSize)
                            .Select(o => new { o.Id, o.TotalAmount })
                            .ToListAsync(stoppingToken);

                if (orders.Count == 0) break;

                totalOrders += orders.Count;
                totalSales += orders.Sum(o => o.TotalAmount);

                page++;
            }

            if (totalOrders > 0)
            {
                var today = DateTime.UtcNow.Date;
                var summary = await db.DailySalesSummaries.FirstOrDefaultAsync(s => s.Date == today, stoppingToken);

                if (summary == null)
                {
                    summary = new DailySalesSummary { Date = today };
                    
                    db.DailySalesSummaries.Add(summary);    
                }

                summary.TotalOrders = totalOrders;
                summary.TotalRevenue = totalSales;


                await db.SaveChangesAsync(stoppingToken);

            }


        }
    }
}
