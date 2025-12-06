using Backend.Data;
using Backend.DTOs;
using Backend.Models;
using Backend.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserRepository _userRepo;
        private readonly IConfiguration _config;

        public AuthController(UserRepository userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { Message = "Passwords do not match." });

            if (_userRepo.EmailExists(dto.Email))
                return Conflict(new { Message = "Email already exists." });

            DateTime? dob = null;
            if (!string.IsNullOrEmpty(dto.DateOfBirth))
                dob = DateTime.Parse(dto.DateOfBirth);

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                DateOfBirth = dob,
                Gender = dto.Gender,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Role = "patient"
            };

            var userId = _userRepo.CreateUser(user);

            return Ok(new { Message = "User registered successfully.", UserId = userId });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginDto dto)
        {
            var user = _userRepo.GetUserByEmail(dto.Email);
            if (user == null)
                return Unauthorized(new { Message = "Invalid email or password." });

            if (!PasswordHasher.Verify(dto.Password, user.PasswordHash))
                return Unauthorized(new { Message = "Invalid email or password." });

            var token = GenerateJwtToken(user);

            return Ok(new { Token = token, User = user });
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("role", user.Role),
                new Claim("name", user.FirstName + " " + user.LastName),
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"])
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
