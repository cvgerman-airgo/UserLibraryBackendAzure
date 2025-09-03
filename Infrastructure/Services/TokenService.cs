using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(Guid userId, string email, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

public DateTime GetTokenExpiration(string token)
        {
            var tokenBytes = Encoding.UTF8.GetBytes(token);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenObject = tokenHandler.ReadJwtToken(token);
            var expiration = tokenObject.Payload.Expiration;

            if (expiration.HasValue)
            {
                return UnixTimeStampToDateTime(expiration.Value);
            }
            else
            {
                // Maneja el caso en que expiration sea nulo
                // Por ejemplo, puedes lanzar una excepción o devolver un valor predeterminado
                throw new InvalidOperationException("La fecha de expiración no se pudo obtener");
            }
        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp);

            return dateTime;
        }

    }
}

