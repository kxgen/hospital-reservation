using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Models;
using Backend.Data;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        // config is all merged settings, _config is the var used to access
        _config = config;
    }

    [HttpPost("register")]
    public IActionResult Register([FromBody] User user) {
        if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.Password))
            return BadRequest(new { Message = "Email and password are required." });
        
        bool userExists = InMemoryStore.Users.Any(
            u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase)
        );

        if (userExists) {
            return Conflict(new { Message = $"Email '{user.Email}' is already taken." });
        }
        var newUser = new User(user.Email, user.Password, "user");
        InMemoryStore.Users.Add(newUser);
        return Ok(new { Message = $"User {user.Email} registered successfully." });
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] User user)
    {
        var validUser = InMemoryStore.Users.FirstOrDefault(u =>
            u.Email == user.Email && u.Password == user.Password);

        if (validUser == null)
            return Unauthorized("Invalid credentials");

        var tokenHandler = new JwtSecurityTokenHandler();

        // Read the key from configuration (appsettings.json)
        // converting jwt key string to raw bytes array
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? throw new Exception("JWT key missing in configuration"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, validUser.Email),
                new Claim(ClaimTypes.Role, validUser.Role)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Ok(new
        {
            token = jwt,
            email = validUser.Email,
            role = validUser.Role
        });
    }
}
