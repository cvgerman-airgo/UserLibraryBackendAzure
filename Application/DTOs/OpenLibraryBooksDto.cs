// Application/DTOs/OpenLibraryBookDto.cs
using System;

namespace Application.DTOs
{
    public class OpenLibraryBookDto
    {
        public string? Title { get; set; }
        public string? Author { get; set; }           // Nombre del/los autores
        public string? Publisher { get; set; }        // Editorial principal
        public string? Summary { get; set; }          // Descripción, si existe
        public int? PageCount { get; set; }           // Número de páginas
        public DateTime? PublicationDate { get; set; }// Fecha de publicación (parseada)
        public string? Language { get; set; }         // Idioma en formato /languages/eng
        public string? Genre { get; set; }            // Categorías si se extraen más adelante
    }
}
