using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;

namespace Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            IUserRepository userRepository,
            IConfiguration configuration,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string?> AuthenticateAsync(string email, string password)
        {
            _logger.LogInformation("🟡 Intentando autenticar al usuario: {Email}", email);

            try
            {
                var user = await _userRepository.GetByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("🔴 Autenticación fallida: usuario no encontrado - {Email}", email);
                    return null;
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    _logger.LogWarning("🔴 Autenticación fallida: contraseña incorrecta - {Email}", email);
                    return null;
                }

                if (!user.EmailConfirmed)
                {
                    _logger.LogWarning("Login fallido: Email no verificado para {Email}", email);
                    return null;
                }

                // Validar configuración JWT
                var jwtKey = _configuration["Jwt:Key"];
                var jwtIssuer = _configuration["Jwt:Issuer"];
                var jwtAudience = _configuration["Jwt:Audience"];

                if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
                {
                    _logger.LogError("Configuración JWT inválida. Key, Issuer o Audience están vacíos.");
                    throw new InvalidOperationException("Configuración JWT inválida");
                }

                // Validar datos usuario importantes para token
                if (user.Id == Guid.Empty || string.IsNullOrEmpty(user.Email) || string.IsNullOrEmpty(user.Role))
                {
                    _logger.LogError("Datos del usuario incompletos para crear token (Id, Email, Role).");
                    throw new InvalidOperationException("Datos del usuario incompletos");
                }

                _logger.LogInformation("🟢 Autenticación correcta para {Email}", email);

                var claims = new[]
                {
            new Claim(ClaimTypes.Name, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Generar token
                // Configuracion del token de autenticación
                // El token expira en 2 horas
                // El token es firmado con la clave de encriptación
                // Se utiliza el algoritmo de firma HmacSha256
                // se guardara en la base de datos de Redis
                var token = new JwtSecurityToken(
                    issuer: jwtIssuer,
                    audience: jwtAudience,
                    claims: claims,
                    expires: DateTime.UtcNow.AddHours(2),
                    signingCredentials: creds);

                return new JwtSecurityTokenHandler().WriteToken(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error inesperado en AuthenticateAsync para el usuario {Email}", email);
                throw; // Re-lanzar para que el controlador lo capture y devuelva 500
            }
        }

        public bool VerifyPassword(string password, string passwordHash)
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }

        public string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }
    }
}
