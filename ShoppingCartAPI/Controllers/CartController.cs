using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ShoppingCartAPI.Data;
using ShoppingCartAPI.Data.Entities;
using ShoppingCartAPI.Dtos;
using System.Security.Claims;

namespace ShoppingCartAPI.Controllers
{
   
    [ApiController]
    [Route("api/[controller]")]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(AppDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        [HttpPost("create")]
        public async Task<IActionResult> CreateCart()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                {
                    _logger.LogWarning("CreateCart called but token did not contain a user ID.");
                    return Unauthorized("User ID not found in token.");
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogWarning("CreateCart called with invalid user ID in token: {TokenValue}", userIdClaim);
                    return Unauthorized("Invalid user ID format.");
                }

                _logger.LogInformation("CreateCart called by user {UserId} at {Time}", userId, DateTime.UtcNow);

                var user = await _context.Users
                    .Include(u => u.CartItems)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("CreateCart: User with ID {UserId} not found.", userId);
                    return NotFound("User not found.");
                }

                var itemCount = user.CartItems.Count;
                user.CartItems.Clear();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cart reset successful for user {UserId}. {Count} items were removed.", userId, itemCount);
                return Ok("Shopping cart created/reset.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating/resetting the cart.");
                return StatusCode(500, "Something went wrong.");
            }
        }


        // 2. Get shopping cart
        [Authorize]
        [HttpGet("getcartitems")]
        public async Task<IActionResult> GetCartItems()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized("Invalid token.");

                if (!int.TryParse(userIdClaim, out int userId))
                    return Unauthorized("Invalid user ID in token.");

                _logger.LogInformation("User {UserId} requested cart items at {Time}", userId, DateTime.UtcNow);

                var cartItems = await _context.CartItems
                    .Where(ci => ci.UserId == userId)
                    .Include(ci => ci.Product)
                    .ToListAsync();

                var cartItemDtos = cartItems.Select(ci => new CartItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    Price = ci.Product.Price
                }).ToList();

                return Ok(new
                {
                    Items = cartItemDtos,
                    Total = cartItemDtos.Sum(x => x.Price * x.Quantity)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred in GetCartItems.");
                return StatusCode(500, "Something went wrong.");
            }
        }

        // 3. Add item to cart
        [Authorize]
        [HttpPost("add")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                {
                    _logger.LogWarning("AddToCart: Missing user ID in token.");
                    return Unauthorized("User ID not found in token.");
                }

                int userId = int.Parse(userIdClaim);

                var user = await _context.Users
                    .Include(u => u.CartItems)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("AddToCart: User with ID {UserId} not found.", userId);
                    return NotFound("User not found.");
                }

                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null)
                {
                    _logger.LogWarning("AddToCart: Product with ID {ProductId} not found.", dto.ProductId);
                    return NotFound("Product not found.");
                }

                var cartItem = user.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

                if (cartItem != null)
                {
                    cartItem.Quantity += dto.Quantity;
                    _logger.LogInformation("AddToCart: Increased quantity of Product {ProductId} in User {UserId}'s cart by {Quantity}.", dto.ProductId, userId, dto.Quantity);
                }
                else
                {
                    user.CartItems.Add(new CartItem
                    {
                        ProductId = dto.ProductId,
                        Quantity = dto.Quantity,
                        UserId = user.Id
                    });

                    _logger.LogInformation("AddToCart: Added Product {ProductId} (Quantity: {Quantity}) to User {UserId}'s cart.", dto.ProductId, dto.Quantity, userId);
                }

                await _context.SaveChangesAsync();
                return Ok("Item added to cart.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddToCart: An error occurred while adding Product {ProductId} to cart.", dto.ProductId);
                return StatusCode(500, "Something went wrong.");
            }
        }




        //  4. Remove item from cart
        [Authorize]
        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveFromCart([FromBody] RemoveFromCartDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                {
                    _logger.LogWarning("RemoveFromCart: Unauthorized access attempt - no user ID in token.");
                    return Unauthorized("User ID not found in token.");
                }

                int userId = int.Parse(userIdClaim);

                var user = await _context.Users
                    .Include(u => u.CartItems)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("RemoveFromCart: User with ID {UserId} not found.", userId);
                    return NotFound("User not found.");
                }

                var cartItem = user.CartItems.FirstOrDefault(ci => ci.ProductId == dto.ProductId);

                if (cartItem == null)
                {
                    _logger.LogWarning("RemoveFromCart: Product ID {ProductId} not found in User {UserId}'s cart.", dto.ProductId, userId);
                    return NotFound("Item not found in your cart.");
                }

                user.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation("RemoveFromCart: Product ID {ProductId} removed from User {UserId}'s cart.", dto.ProductId, userId);
                return Ok("Item removed from cart.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RemoveFromCart: An error occurred while removing Product ID {ProductId} from User {UserId}'s cart.", dto.ProductId);
                return StatusCode(500, "Something went wrong.");
            }
        }


        // 5. Checkout cart
        [Authorize]
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                {
                    _logger.LogWarning("Checkout: Unauthorized access attempt - user ID missing in token.");
                    return Unauthorized("Invalid token.");
                }

                int userId = int.Parse(userIdClaim);

                var user = await _context.Users
                    .Include(u => u.CartItems)
                        .ThenInclude(ci => ci.Product)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("Checkout: User with ID {UserId} not found.", userId);
                    return NotFound("User not found.");
                }

                if (!user.CartItems.Any())
                {
                    _logger.LogInformation("Checkout: User ID {UserId} attempted checkout with an empty cart.", userId);
                    return BadRequest("Cart is empty.");
                }

                foreach (var item in user.CartItems)
                {
                    if (item.Product.Stock < item.Quantity)
                    {
                        _logger.LogWarning("Checkout: Not enough stock for Product '{ProductName}' (ID: {ProductId}) - Requested: {Requested}, Available: {Available}",
                            item.Product.Name, item.ProductId, item.Quantity, item.Product.Stock);
                        return BadRequest($"Not enough stock for {item.Product.Name}.");
                    }

                    item.Product.Stock -= item.Quantity;
                }

                user.CartItems.Clear();
                await _context.SaveChangesAsync();

                _logger.LogInformation("Checkout: User ID {UserId} successfully placed an order at {Time}.", userId, DateTime.UtcNow);

                return Ok("Checkout successful. Order placed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Checkout: Error occurred while processing checkout for user ID {UserId}.", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, "Something went wrong.");
            }
        }


    }

}
