using Infrastructure.BackgroundJob;
using Infrastructure.Caching;
using Infrastructure .Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure .Extensions
{
    public static class InfrastructureDependencyInjectionHandler
    {
        public static IServiceCollection AddInfrastructureDependencyInjection(this IServiceCollection services,IConfiguration configuration)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer("Server=.;Database=ParallelProject;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
            });

            services.AddSingleton<IBackgroundTaskQueue>(provider => 
                    new BackgroundTaskQueue(capacity: 100));

            services.AddHostedService<QueuedHostService>();

            services.AddHostedService<DailySalesBatchService>();

            var redisConnectionString = configuration.GetConnectionString("Redis");
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
                options.InstanceName = "GeniusesProMax:";
            });

            services.AddSingleton<ICacheService, RedisCacheService>();

            return services;
        }
    }
}
