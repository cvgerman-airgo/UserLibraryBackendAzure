using Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs;

public class UpdateBookRequest
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
    public ReadingStatus? Status { get; set; }
    public string? LentTo { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public byte[]? CoverImage { get; set; }
}

