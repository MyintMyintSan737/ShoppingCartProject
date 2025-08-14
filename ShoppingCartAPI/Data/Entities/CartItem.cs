using System.ComponentModel.DataAnnotations;

namespace ShoppingCartAPI.Data.Entities
{
    public class CartItem
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        // Navigation properties
        public Users? User { get; set; }
        public Product? Product { get; set; }
    }
}
