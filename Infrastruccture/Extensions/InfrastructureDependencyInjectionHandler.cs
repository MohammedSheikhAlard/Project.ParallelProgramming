using Infrastructure.BackgroundJob;
using Infrastructure .Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure .Extensions
{
    public static class InfrastructureDependencyInjectionHandler
    {
        public static IServiceCollection AddInfrastructureDependencyInjection(this IServiceCollection services)
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlServer("Server=.;Database=ParallelProject;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true");
            });

            services.AddSingleton<IBackgroundTaskQueue>(provider => 
                    new BackgroundTaskQueue(capacity: 100));

            services.AddHostedService<QueuedHostService>();

            services.AddHostedService<DailySalesBatchService>();

            return services;
        }
    }
}
