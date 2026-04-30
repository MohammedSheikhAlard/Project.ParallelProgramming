using GeniusesProMax.Interfaces;
using GeniusesProMax.Services;

namespace GeniusesProMax
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddDependencyIndection(this IServiceCollection services)
        {
            services.AddScoped<IAuthService,AuthService>();
            services.AddScoped<IProductService, ProductService>();  
            services.AddScoped<IOrderService, OrderService>();
            services.AddScoped<ICartService, CartService>();

            return services;
        }
    }
}
