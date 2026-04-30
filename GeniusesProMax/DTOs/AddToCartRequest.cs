using System.ComponentModel.DataAnnotations;

namespace GeniusesProMax.DTOs
{
    public class AddToCartRequest
    {
        [Required, Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Required, Range(1, 1000)]
        public int Quantity { get; set; }
    }


    public class UpdateCartItemRequest
    {
        [Required, Range(1, 1000)]
        public int Quantity { get; set; }
    }


}
