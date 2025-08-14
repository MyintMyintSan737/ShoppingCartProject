using Microsoft.AspNetCore.Mvc;
using ShoppingCartAPI.Data.Entities;
using ShoppingCartAPI.Dtos;
using ShoppingCartAPI.Data;
namespace ShoppingCartAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(AppDbContext context, ILogger<ProductController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            _logger.LogInformation("CreateProduct called for product: {ProductName}", dto.Name);

            if (string.IsNullOrWhiteSpace(dto.Name) || dto.Price <= 0 || dto.Stock < 0)
                return BadRequest("Invalid product data.");

            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock
            };

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Product created: {ProductName} (ID: {ProductId})", product.Name, product.Id);

            return Ok(new
            {
                message = "Product created successfully",
                productId = product.Id
            });
        }
    }

}
