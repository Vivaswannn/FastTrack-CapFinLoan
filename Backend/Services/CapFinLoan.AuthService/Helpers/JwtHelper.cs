using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CapFinLoan.AuthService.Models;
using Microsoft.IdentityModel.Tokens;

namespace CapFinLoan.AuthService.Helpers
{
    /// <summary>
    /// Helper class for generating JWT tokens.
    /// Reads configuration from appsettings.json JwtSettings section.
    /// </summary>
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a signed JWT token for the given user.
        /// Token contains: UserId, Email, Role as claims.
        /// Signed with HMAC SHA256 using SecretKey from config.
        /// </summary>
        /// <param name="user">The authenticated user</param>
        /// <returns>Signed JWT token string</returns>
        public string GenerateToken(User user)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]
                ?? throw new InvalidOperationException("JWT SecretKey not configured");
            var issuer = jwtSettings["Issuer"]
                ?? throw new InvalidOperationException("JWT Issuer not configured");
            var audience = jwtSettings["Audience"]
                ?? throw new InvalidOperationException("JWT Audience not configured");
            var expiryMinutes = int.Parse(jwtSettings["ExpiryMinutes"] ?? "60");

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            // Claims are pieces of information stored inside the token
            // Every request sends these claims back to the server
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("role", user.Role),
                new Claim("name", user.FullName),
                new Claim(JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat,
                    DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    ClaimValueTypes.Integer64)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Gets the expiry time for a token generated right now.
        /// Used to return ExpiresAt in AuthResponseDto.
        /// </summary>
        public DateTime GetExpiryTime()
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var expiryMinutes = int.Parse(
                jwtSettings["ExpiryMinutes"] ?? "60");
            return DateTime.UtcNow.AddMinutes(expiryMinutes);
        }
    }
}
