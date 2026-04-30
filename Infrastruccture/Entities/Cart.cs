namespace Infrastruccture.Entities
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }


}
