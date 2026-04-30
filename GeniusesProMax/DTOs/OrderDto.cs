namespace GeniusesProMax.DTOs
{
    public class OrderDto
    {
        public int Id { get; set; } 
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = default!;
        public decimal TotalAmount { get; set; }
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public string ProductName { get; set; } = default!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }
    
}
