using Domain.Entities;
using System;

namespace Application.DTOs;



public class BookDto
{
    public Guid Id { get; set; }  
    public Guid? UserId { get; set; } // Se asigna en el backend manualmente si proviene de Google/OpenLibrary
    public string Title { get; set; } = default!;
    public string? Series { get; set; }
    public string? ISBN { get; set; }
    public string? CoverUrl { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string Author { get; set; } = default!;
    public string? Publisher { get; set; }
    public string? Genre { get; set; }
    public string? Summary { get; set; }
    public DateTime? PublicationDate { get; set; }
    public int? PageCount { get; set; }
    public string? Country { get; set; } // Código ISO de idioma, por ej. "es", "en"
    public string? Language { get; set; } // Código ISO de idioma, por ej. "es", "en"
    public DateTime AddedDate { get; set; }
    public DateTime? StartReadingDate { get; set; }
    public DateTime? EndReadingDate { get; set; }
    public ReadingStatus Status { get; set; } = ReadingStatus.NotRead;
    public string? LentTo { get; set; }
    public byte[]? CoverImage { get; set; }
}

