using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Nexora.Api.Models;

namespace Nexora.Api.Utils
{
    public static class JwtHelper
    {
        public static string GenerateToken(ApplicationUser user, IConfiguration config)
        {
            var issuer = config["Jwt:Issuer"] ?? "Nexora";
            var audience = config["Jwt:Audience"] ?? "Nexora";
            var key = config["Jwt:Key"] ?? throw new ArgumentNullException("Jwt:Key is missing in configuration.");
            var expiresMinutes = int.Parse(config["Jwt:ExpiresMinutes"] ?? "60");

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty)
            };

            var keyBytes = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(keyBytes, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
