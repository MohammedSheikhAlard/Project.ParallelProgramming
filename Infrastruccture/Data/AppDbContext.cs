using Infrastruccture.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users => Set<User>();
        public DbSet<Product> Products => Set<Product>();
        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();
        public DbSet<Order> Orders => Set<Order>();
        public DbSet<OrderItem> OrderItems => Set<OrderItem>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ---------- User ----------
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // ---------- Product ----------
            modelBuilder.Entity<Product>(entity =>
            {
                // The [Timestamp] attribute is already picked up by convention,
                // but we make it explicit here for clarity.
                entity.Property(p => p.RowVersion)
                      .IsRowVersion()
                      .IsConcurrencyToken();
            });

            // ---------- Cart ----------
            modelBuilder.Entity<Cart>(entity =>
            {
                entity.HasOne(c => c.User)
                      .WithOne(u => u.Cart)
                      .HasForeignKey<Cart>(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- CartItem ----------
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasOne(ci => ci.Cart)
                      .WithMany(c => c.Items)
                      .HasForeignKey(ci => ci.CartId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(ci => ci.Product)
                      .WithMany()
                      .HasForeignKey(ci => ci.ProductId)
                      .OnDelete(DeleteBehavior.Restrict); // don't cascade product delete to cart items
            });

            // ---------- Order ----------
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasOne(o => o.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(o => o.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // ---------- OrderItem ----------
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasOne(oi => oi.Order)
                      .WithMany(o => o.Items)
                      .HasForeignKey(oi => oi.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(oi => oi.Product)
                      .WithMany()
                      .HasForeignKey(oi => oi.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ---------- Seed products ----------
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Price = 1200.00m, StockQuantity = 10 },
                new Product { Id = 2, Name = "Smartphone", Price = 800.00m, StockQuantity = 20 },
                new Product { Id = 3, Name = "Headphones", Price = 150.00m, StockQuantity = 50 },
                new Product { Id = 4, Name = "Monitor", Price = 300.00m, StockQuantity = 15 },
                new Product { Id = 5, Name = "Keyboard", Price = 100.00m, StockQuantity = 30},
                new Product { Id = 6, Name = "Mouse", Price = 50.00m, StockQuantity = 40 },
                new Product { Id = 7, Name = "Printer", Price = 200.00m, StockQuantity = 10 },
                new Product { Id = 8, Name = "Webcam", Price = 80.00m, StockQuantity = 25 },
                new Product { Id = 9, Name = "Speakers", Price = 120.00m, StockQuantity = 20 }
            );


            // ---------- Seed users ----------
            // Password for both is "password123"
            var passwordHash = ComputeSha256Hash("password123");

            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@ecommerce.com",
                    Role = "Admin",
                    PasswordHash = passwordHash
                },
                new User
                {
                    Id = 2,
                    Username = "user1",
                    Email = "user1@ecommerce.com",
                    Role = "User",
                    PasswordHash = passwordHash
                }
            );
        }

        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes);
        }
    }
}
