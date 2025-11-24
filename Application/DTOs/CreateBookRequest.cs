using Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class CreateBookRequest
{
    [Required]
    public string Title { get; set; } = default!;
    public string? Series { get; set; }
    public string? ISBN { get; set; }

    [Required]
    public string Author { get; set; } = default!;
    public string? Publisher { get; set; }
    public string? Genre { get; set; }
    public string? Summary { get; set; }
    public DateTime? PublicationDate { get; set; }
    public int? PageCount { get; set; }
    public DateTime? StartReadingDate { get; set; }
    public DateTime? EndReadingDate { get; set; }
    [EnumDataType(typeof(ReadingStatus))]
    public ReadingStatus Status { get; set; } = ReadingStatus.NotRead; // 0: No leído, 1: Leyendo, 2: Terminado, 3: No terminado
    public string? LentTo { get; set; }
    public byte[]? CoverImage { get; set; }
}

