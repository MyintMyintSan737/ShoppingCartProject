using System.ComponentModel.DataAnnotations;

namespace ShoppingCartAPI.Data.Entities
{
    public class Users
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public List<CartItem> CartItems { get; set; } = new();

    }
}
