using Application.DTOs;
using System.Globalization;

namespace Infrastructure.Extensions
{
    public static class GoogleBookItemExtensions
    {
        

        
        public static BookDto ToBookDto(this GoogleBookItem item)
        {

            try
            {
                var volume = item.VolumeInfo;
                var access = item.AccessInfo;

                string? isbn = volume?.IndustryIdentifiers?
                    .FirstOrDefault(id => id.Type == "ISBN_13")?.Identifier
                    ?? volume?.IndustryIdentifiers?
                    .FirstOrDefault(id => id.Type == "ISBN_10")?.Identifier;

                DateTime? publicationDate = null;
                if (DateTime.TryParseExact(volume?.PublishedDate, new[] { "yyyy-MM-dd", "yyyy-MM", "yyyy" },
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    publicationDate = parsedDate.ToUniversalTime();
                }

//                Console.WriteLine($"📕 ISBN extraído: {isbn}");
//                Console.WriteLine($"📕 Título extraído: {volume?.Title}");
//                Console.WriteLine($"📕 Autor extraído: {string.Join(", ", volume?.Authors ?? new List<string>())}");
//                Console.WriteLine($"📕 Imagen extraída: {volume?.ImageLinks?.Thumbnail}");

                return new BookDto
                {
                    ISBN = isbn,
                    Title = volume?.Title,
                    Author = volume?.Authors != null && volume.Authors.Any()
                        ? string.Join(", ", volume.Authors)
                        : "Sin autor",
                    Publisher = volume?.Publisher,
                    Summary = !string.IsNullOrWhiteSpace(volume?.Description)
                        ? volume.Description
                        : null,
                    Genre = volume?.Categories != null ? string.Join(", ", volume.Categories) : null,
                    PageCount = volume?.PageCount,
                    PublicationDate = publicationDate,
                    Language = volume?.Language,
                    Country = access?.Country,
                    CoverUrl = volume?.ImageLinks?.Thumbnail,
                    ThumbnailUrl = null // Se rellenará después con el servicio de imagen
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al convertir GoogleBookItem a BookDto: {ex.Message}");
                // Aquí podrías registrar el error si tienes un sistema de logging
                // Por ejemplo: _logger.LogError(ex, "Error al convertir GoogleBookItem a BookDto");
                return new BookDto(); // Retorna un objeto vacío en caso de error
            }
            ;
        }
    }
}

