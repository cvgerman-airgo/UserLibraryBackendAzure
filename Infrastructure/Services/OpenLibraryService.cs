using System.Text.Json;
using Application.DTOs;
using Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class OpenLibraryService : IOpenLibraryService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenLibraryService> _logger;
        private readonly IImageService _imageService;

        public OpenLibraryService(HttpClient httpClient, ILogger<OpenLibraryService> logger, IImageService imageService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _imageService = imageService;
        }

        public async Task<BookDto?> GetByIsbnAsync(string isbn)
        {
            var url = $"https://openlibrary.org/isbn/{isbn}.json";
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;

                string? fullCoverUrl = null;
                string? thumbnailCoverUrl = null;

                var coverUrl = $"https://covers.openlibrary.org/b/isbn/{isbn}-L.jpg";

                try
                {
                    (fullCoverUrl, thumbnailCoverUrl) = await _imageService.DownloadAndSaveCoverAsync(coverUrl, isbn);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Error al descargar portadas de OpenLibrary: {Message}", ex.Message);
                }

                // 🧠 Obtener nombres reales de autores
                string? authorNames = null;
                if (root.TryGetProperty("authors", out var authorsArray) && authorsArray.ValueKind == JsonValueKind.Array)
                {
                    var names = new List<string>();
                    foreach (var authorRef in authorsArray.EnumerateArray())
                    {
                        var key = authorRef.GetProperty("key").GetString(); // "/authors/OL12345A"
                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            var authorUrl = $"https://openlibrary.org{key}.json";
                            try
                            {
                                var authorResponse = await _httpClient.GetAsync(authorUrl);
                                if (authorResponse.IsSuccessStatusCode)
                                {
                                    var authorJson = await authorResponse.Content.ReadAsStringAsync();
                                    var authorRoot = JsonDocument.Parse(authorJson).RootElement;
                                    if (authorRoot.TryGetProperty("name", out var nameProp))
                                        names.Add(nameProp.GetString()!);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("⚠️ Error obteniendo nombre del autor {AuthorUrl}: {Message}", authorUrl, ex.Message);
                            }
                        }
                    }

                    if (names.Count > 0)
                        authorNames = string.Join(", ", names);
                }

                // 🧠 Obtener descripción
                string? summary = null;
                if (root.TryGetProperty("description", out var descProp))
                {
                    if (descProp.ValueKind == JsonValueKind.String)
                        summary = descProp.GetString();
                    else if (descProp.ValueKind == JsonValueKind.Object &&
                             descProp.TryGetProperty("value", out var descValue))
                        summary = descValue.GetString();
                }

                return new BookDto
                {
                    ISBN = isbn,
                    Title = root.TryGetProperty("title", out var title) ? title.GetString() : null,
                    Author = authorNames,
                    Publisher = root.TryGetProperty("publishers", out var pubs) && pubs.ValueKind == JsonValueKind.Array
                        ? pubs[0].GetString()
                        : null,
                    PageCount = root.TryGetProperty("number_of_pages", out var pages) ? pages.GetInt32() : (int?)null,
                    Summary = summary,
                    PublicationDate = root.TryGetProperty("publish_date", out var pubDate) &&
                                      DateTime.TryParse(pubDate.GetString(), out var date)
                        ? date
                        : (DateTime?)null,
                    Language = root.TryGetProperty("languages", out var langs) && langs.ValueKind == JsonValueKind.Array
                        ? string.Join(", ", langs.EnumerateArray().Select(l => l.GetProperty("key").GetString()))
                        : null,
                    CoverUrl = fullCoverUrl,
                    ThumbnailUrl = thumbnailCoverUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning("❌ Error al consultar o procesar OpenLibrary: {Message}", ex.Message);
                return null;
            }
        }


    }
}
