using Domain.Entities;

namespace Application.DTOs;
public class ImportBookFromGoogleRequest
{
    public string ISBN { get; set; } = string.Empty;
    public string? Language { get; set; }
    public string? Country { get; set; }
}
