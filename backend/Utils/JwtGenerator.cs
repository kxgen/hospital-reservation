using Backend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Backend.Utils
{
    public class JwtGenerator
    {
        private readonly IConfiguration _config;

        public JwtGenerator(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateJwtToken(Account account, string fullName)
        {
            
            if (account == null) throw new ArgumentNullException(nameof(account));
            
            var jwtKey = _config["Jwt:Key"] ?? throw new Exception("JWT Key is missing in configuration.");
            var issuer = _config["Jwt:Issuer"];
            var audience = _config["Jwt:Audience"];
            
            var role = account.RoleName?.ToLower() ?? throw new Exception("User role is missing.");

            
            
            var claims = new List<Claim>
            {
                new Claim("id", account.AccountId.ToString()), 
                new Claim(JwtRegisteredClaimNames.Email, account.Email),
                new Claim(JwtRegisteredClaimNames.Name, fullName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), 
                new Claim("role", role) 
            };

            
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            
            var tokenDescriptor = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            
            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        }
    }
}