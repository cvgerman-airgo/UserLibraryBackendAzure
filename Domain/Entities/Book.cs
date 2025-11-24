using System.ComponentModel.DataAnnotations;

namespace Domain.Entities
{
    public class Book
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Users User { get; set; } = default!;
        [Required]
        public string Title { get; set; } = string.Empty;
        public string? Series { get; set; }
        public string? ISBN { get; set; }
        public string? CoverUrl { get; set; }   // Ruta relativa a imagen grande
//        public string? CoverFullPath { get; set; } // Ruta relativa a miniatura
        public string? ThumbnailUrl { get; set; }
        public byte[]? CoverImage { get; set; }
        [Required]
        public string Author { get; set; } = string.Empty;
        public string? Publisher { get; set; }
        public string? Genre { get; set; }
        public string? Summary { get; set; }
        public DateTime? PublicationDate { get; set; }
        public int? PageCount { get; set; }
        public string? Language { get; set; }
        public string? Country { get; set; }


        // Datos de gestión personal
        public DateTime AddedDate { get; set; } = DateTime.UtcNow;
        public DateTime? StartReadingDate { get; set; }
        public DateTime? EndReadingDate { get; set; }
        public ReadingStatus Status { get; set; } = ReadingStatus.NotRead;
        public string? LentTo { get; set; }
       
    }
}

