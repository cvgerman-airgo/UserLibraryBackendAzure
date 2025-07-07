using Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Application.Helpers
{
    public static class BookDataMerger
    {
        public static BookDto Merge(BookDto? googleBook, BookDto? openLibraryBook, ILogger logger)
        {
            
//            Console.WriteLine("🔍 Iniciando el merge de datos de libros...");
//            logger.LogInformation("🔍 Datos de Google Book: {json}", JsonSerializer.Serialize(googleBook));
//            logger.LogInformation("🔍 Datos de Open Library: {json}", JsonSerializer.Serialize(openLibraryBook));

            var merged = new BookDto
            {

                ISBN = FirstNonEmpty(googleBook?.ISBN, openLibraryBook?.ISBN),
                Title = FirstNonEmpty(googleBook?.Title, openLibraryBook?.Title),
                Author = FirstNonEmpty(googleBook?.Author, openLibraryBook?.Author),
                Publisher = FirstNonEmpty(googleBook?.Publisher, openLibraryBook?.Publisher),
                Summary = FirstNonEmpty(googleBook?.Summary, openLibraryBook?.Summary),
                Genre = FirstNonEmpty(googleBook?.Genre, openLibraryBook?.Genre),
                PageCount = googleBook?.PageCount ?? openLibraryBook?.PageCount,
                PublicationDate = googleBook?.PublicationDate ?? openLibraryBook?.PublicationDate,
                Language = FirstNonEmpty(googleBook?.Language, openLibraryBook?.Language),
                Country = FirstNonEmpty(googleBook?.Country, openLibraryBook?.Country),
                CoverUrl = FirstNonEmpty(googleBook?.CoverUrl, openLibraryBook?.CoverUrl),
                ThumbnailUrl = FirstNonEmpty(googleBook?.ThumbnailUrl, openLibraryBook?.ThumbnailUrl),
                UserId = googleBook?.UserId ?? openLibraryBook?.UserId
            };

            // 🚀 Defaults para campos críticos
            if (string.IsNullOrWhiteSpace(merged.Title))
                merged.Title = "Sin titulo";
            if (string.IsNullOrWhiteSpace(merged.Author))
                merged.Author = "Sin autor";
            if (string.IsNullOrWhiteSpace(merged.CoverUrl))
                merged.CoverUrl = "/covers/default.jpg";
            if (string.IsNullOrWhiteSpace(merged.ThumbnailUrl))
                merged.ThumbnailUrl = "/covers/default_thumb.jpg";

            //logger.LogInformation("🔍 Resultado del merge final extendido: {json}", JsonSerializer.Serialize(merged));


            return merged;
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
        }
    }
}
