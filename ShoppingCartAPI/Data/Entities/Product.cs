using System.ComponentModel.DataAnnotations;

namespace ShoppingCartAPI.Data.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        public int Stock { get; set; }
    }

}
