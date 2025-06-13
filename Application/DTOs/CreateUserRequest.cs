using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateUserRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MaxLength(100)]
    public string Name { get; set; } = default!;

    [Required, MinLength(6)]
    public string Password { get; set; } = default!;
}


