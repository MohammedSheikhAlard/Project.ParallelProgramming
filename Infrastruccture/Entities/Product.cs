using System.ComponentModel.DataAnnotations;

namespace Infrastruccture.Entities
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public decimal Price { get; set; }

        // The shared resource that needs concurrency protection
        public int StockQuantity { get; set; }

        // EF Core concurrency token – will be mapped as rowversion
        [Timestamp]
        public byte[] RowVersion { get; set; } = default!;
    }


}
