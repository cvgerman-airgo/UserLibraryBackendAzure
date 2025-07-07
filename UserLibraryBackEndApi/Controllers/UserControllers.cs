using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace UserLibraryBackEndApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;
    private readonly IRedisService _redisService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;

    public UsersController(
        IUserRepository userRepository, 
        IAuthService authService,
        ILogger<UsersController> logger,
        IRedisService redisService,
        IEmailService emailService,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _authService = authService;
        _logger = logger;
        _redisService = redisService;
        _emailService = emailService;
        _configuration = configuration;
    }
    //[HttpPost("init-admin")]
    //[AllowAnonymous]
    //public async Task<IActionResult> InitAdmin()
    //{
    //    if (await _userRepository.ExistsByEmailAsync("german.cv@gmail.com"))
    //        return Conflict("Ya existe un admin.");

    //    var user = new Users
    //    {
    //        Id = Guid.NewGuid(),
    //        Email = "german.cv@gmail.com",
    //        Name = "Administrador",
    //        PasswordHash = _authService.HashPassword("13571357"),
    //        CreatedAt = DateTime.UtcNow,
    //        IsActive = true,
    //        Role = "admin",
    //        EmailConfirmed = true
    //    };
    //    await _userRepository.AddAsync(user);
    //    return Ok("Admin creado.");
    //}

    [HttpGet]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _userRepository.GetAllAsync();

        var userDtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            Name = u.Name,
            Email = u.Email,
            Role = u.Role,
            CreatedAt = u.CreatedAt
        }).ToList();

        return Ok(userDtos);
    }

    // GET: api/users/me
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        var email = User.Identity?.Name;
        if (email == null)
        {
            _logger.LogWarning("GetMe: Usuario no autenticado.");
            return Unauthorized();
        }

        _logger.LogInformation("GetMe: solicitando datos para {Email}", email);

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("GetMe: Usuario no encontrado para {Email}", email);
            return NotFound();
        }

        _logger.LogInformation("GetMe: datos devueltos para {Email}", email);
        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt,
            Role = user.Role
        });
    }


    // PUT: api/users/me
    [HttpPut("me")]
    [Authorize]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserRequest request)
    {
        var email = User.Identity?.Name;
        if (email == null)
        {
            _logger.LogWarning("UpdateMyProfile: Usuario no autenticado.");
            return Unauthorized();
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("UpdateMyProfile: Usuario no encontrado para {Email}", email);
            return NotFound();
        }

        user.Name = request.Name;
        user.PasswordHash = _authService.HashPassword(request.Password);

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Perfil actualizado correctamente para {Email}", email);
        return NoContent();
    }

    // POST: api/users
    [HttpPost]
    [Authorize (Roles = "admin")]
    public async Task<ActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        _logger.LogInformation("Intento de creación de usuario para {Email}", request.Email);

        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            _logger.LogWarning("CreateUser: Email ya registrado para {Email}", request.Email);
            return Conflict("El email ya está registrado.");
        }

        var passwordHash = _authService.HashPassword(request.Password);

        var user = new Users
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Name = request.Name,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = "user" // Asigna el rol por defecto
        };

        await _userRepository.AddAsync(user);

        _logger.LogInformation("Usuario creado correctamente: {Email}", request.Email);
        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id });
    }

    // PUT: api/users/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request)
    {
        _logger.LogInformation("Intento de actualización de usuario con Id {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("UpdateUser: Usuario no encontrado con Id {UserId}", id);
            return NotFound();
        }

        user.Name = request.Name;
        user.PasswordHash = _authService.HashPassword(request.Password);

        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Usuario actualizado con éxito con Id {UserId}", id);
        return NoContent();
    }

    // DELETE: api/users/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        _logger.LogInformation("Intento de eliminación lógica de usuario con Id {UserId}", id);

        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            _logger.LogWarning("DeleteUser: Usuario no encontrado con Id {UserId}", id);
            return NotFound();
        }

        user.IsActive = false;
        await _userRepository.UpdateAsync(user);

        _logger.LogInformation("Usuario desactivado con éxito con Id {UserId}", id);
        return NoContent();
    }

    // GET: api/users/{id}
    [HttpGet("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<UserDto>> GetUser(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            Name = user.Name,
            CreatedAt = user.CreatedAt,
            Role = user.Role
        });
    }
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        _logger.LogInformation("Intento de login para {Email}", request.Email);

        try
        {
            var token = await _authService.AuthenticateAsync(request.Email, request.Password);

            if (token == null)
            {
                _logger.LogWarning("Login fallido para {Email}", request.Email);
                return Unauthorized("Email o contraseña incorrectos.");
            }

            _logger.LogInformation("Login correcto para {Email}", request.Email);
            return Ok(new { token });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado al intentar hacer login para {Email}", request.Email);
            return StatusCode(500, "Error interno del servidor.");
        }
    }

    // POST: api/users/forgot-password
    // Este endpoint se usa para solicitar un enlace de restablecimiento de contraseña
    // Se espera que el email se envíe en el cuerpo de la solicitud
    // Se verifica si el email existe en la base de datos
    // Si existe, se genera un token y se almacena en Redis
    // Se envía un enlace al email con el token
    // Si no existe, se devuelve un error
    // 400 Bad Request
    // 401 Unauthorized
    //...........
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            _logger.LogWarning("Solicitud de restablecimiento: Email no encontrado: {Email}", request.Email);
            return Ok("Si el email {Email} existe, se ha enviado un enlace para restablecer la contraseña.");
        }
        try
        {
            Console.WriteLine(" 279 Enviando email a: " + user.Email);
        var token = Guid.NewGuid().ToString();
        var key = $"reset:{token}";
        await _redisService.SetTokenAsync(key, user.Id.ToString(), TimeSpan.FromMinutes(15));
            Console.WriteLine(" 283 Enviando email a: " + user.Email);
            var baseUrl = _configuration["Frontend:BaseUrl"];
            var resetLink = $"{baseUrl}/reset-password?token={token}";
            _logger.LogInformation("Token generado: {Token}", token);
        _logger.LogInformation("Enlace de recuperación: {ResetLink}", resetLink);

            Console.WriteLine(" 288 Enviando email a: " + user.Email);
            Console.WriteLine("📧 Enviando email a: " + user.Email);
            await _emailService.SendAsync(
                user.Email,
                "Restablece tu contraseña",
                $"Haz clic aquí para cambiar tu contraseña: <a href='{resetLink}'>{resetLink}</a>"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Error al enviar el correo: " + ex.Message);
            _logger.LogError(ex, "Error enviando correo a {Email}", user.Email);
            // No devuelvas el error al usuario, mantén el mismo mensaje para no filtrar información
        }

        return Ok("303 Si el email existe, se ha enviado un enlace para restablecer la contraseña.");
    }


    // POST: api/users/reset-password
    // Este endpoint se usa para restablecer la contraseña
    // Se espera que el token se envíe en el cuerpo de la solicitud
    // y la nueva contraseña también
    // Se verifica el token en Redis y se actualiza la contraseña del usuario
    // Si el token es válido, se actualiza la contraseña
    // y se elimina el token de Redis
    // Si el token no es válido, se devuelve un error
    // 400 Bad Request
    // 401 Unauthorized
    // 404 Not Found
    // 500 Internal Server Error
    // 200 OK
    // 201 Created
    // 204 No Content
    // 409 Conflict
    // .........
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var key = $"reset:{request.Token}";
        var userIdStr = await _redisService.GetTokenAsync(key);
        if (userIdStr == null)
        {
            _logger.LogWarning("Token inválido o expirado: {Token}", request.Token);
            return BadRequest("Token inválido o expirado.");
        }

        var userId = Guid.Parse(userIdStr);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        user.PasswordHash = _authService.HashPassword(request.Password);
        await _userRepository.UpdateAsync(user);
        await _redisService.DeleteTokenAsync(key);

        _logger.LogInformation("Contraseña actualizada correctamente para {Email}", user.Email);
        return Ok("Contraseña restablecida correctamente.");
    }

    [HttpPost("request-email-verification")]
    [Authorize]
    public async Task<IActionResult> RequestEmailVerification([FromBody] EmailRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null) return NotFound("Usuario no encontrado.");

        if (user.EmailConfirmed)
            return BadRequest("El email ya está verificado.");

        var token = Guid.NewGuid().ToString();
        var key = $"email-verification:{token}";

        await _redisService.SetAsync(key, user.Id.ToString(), TimeSpan.FromHours(1));

        // Dirección real de tu frontend React
        // Dirección real de tu frontend React
        var backendUrl = _configuration["Frontend:BaseUrl"];
        var verificationLink = $"{backendUrl}/verify-email?token={token}";


        await _emailService.SendAsync(user.Email, "Verifica tu email", $"Haz clic aquí para verificar tu email: {verificationLink}");

        _logger.LogInformation("Token de verificación enviado a {Email}", user.Email);
        return Ok("Se ha enviado un email de verificación.");
    }
    [HttpGet("verify-email")] //GET /verify-email
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        var key = $"email-verification:{token}";
        var userIdStr = await _redisService.GetAsync(key);
        if (userIdStr == null) return BadRequest("Token inválido o expirado.");

        var userId = Guid.Parse(userIdStr);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        user.EmailConfirmed = true;
        await _userRepository.UpdateAsync(user);
        await _redisService.DeleteTokenAsync(key);

        return Ok("Correo electrónico verificado con éxito.");
    }
    [HttpPost("verify-email")] // POST /verify-email
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmailPost([FromBody] VerifyEmailRequest request)
    {
        var key = $"email-verification:{request.Token}";
        var userIdStr = await _redisService.GetAsync(key);
        if (userIdStr == null) return BadRequest("Token inválido o expirado.");

        var userId = Guid.Parse(userIdStr);
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null) return NotFound();

        user.EmailConfirmed = true;
        await _userRepository.UpdateAsync(user);
        await _redisService.DeleteTokenAsync(key);

        return Ok("Correo electrónico verificado con éxito.");
    }

    public class VerifyEmailRequest
    {
        public string Token { get; set; } = default!;
    }


    [HttpPost("register")] // POST /api/users/register
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] CreateUserRequest request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            return Conflict("El email ya está registrado.");
        }

        var passwordHash = _authService.HashPassword(request.Password);
        var user = new Users
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            Name = request.Name,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            Role = "user",
            EmailConfirmed = false
        };

        await _userRepository.AddAsync(user);

        // Opcional: enviar email de verificación
        var token = Guid.NewGuid().ToString();
        var key = $"email-verification:{token}";
        await _redisService.SetAsync(key, user.Id.ToString(), TimeSpan.FromHours(1));

        // Dirección real de tu frontend React
        var backendUrl = _configuration["Frontend:BaseUrl"];
        var verificationLink = $"{backendUrl}/verify-email?token={token}";

        await _emailService.SendAsync(user.Email, "Verifica tu email", $"Haz clic aquí para verificar tu email: {verificationLink}");

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new { user.Id });
    }


    // DTO para exponer solo los campos necesarios
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Role { get; set; } = default!;
        public DateTime CreatedAt { get; set; }
    }

}
