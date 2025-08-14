using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShoppingCartAPI.Data;
using ShoppingCartAPI.Data.Entities;
using ShoppingCartAPI.Dtos;

namespace ShoppingCartAPI.Controllers
{
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserController> _logger;

        public UserController(AppDbContext context, ILogger<UserController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            _logger.LogInformation("CreateUser called for username: {Username}", dto.Username);

            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest("Username and password are required.");

            var existingUser = await _context.Users
                .AnyAsync(u => u.Username == dto.Username);

            if (existingUser)
            {
                _logger.LogWarning("Attempt to create duplicate user: {Username}", dto.Username);
                return Conflict("Username already exists.");
            }

            var user = new Users
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created: {Username}, ID: {UserId}", user.Username, user.Id);
            return Ok(new { message = "User created successfully", userId = user.Id });
        }
    }
}
