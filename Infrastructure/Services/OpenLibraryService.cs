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

        public OpenLibraryService(HttpClient httpClient, ILogger<OpenLibraryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }


        public async Task<List<BookDto>> SearchOpenLibraryAsync(string? title, string? author, string? language)
        {
            var queryParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(title))
                queryParts.Add(title);
            if (!string.IsNullOrWhiteSpace(author))
                queryParts.Add(author);
            var query = string.Join("+", queryParts);

            // Traducir idioma a ISO 639-2 si es necesario
            string? olLanguage = language;
            if (!string.IsNullOrWhiteSpace(language))
            {
                switch (language.ToLower())
                {
                    case "es": olLanguage = "spa"; break;
                    case "en": olLanguage = "eng"; break;
                    case "fr": olLanguage = "fre"; break;
                    case "de": olLanguage = "ger"; break;
                    case "it": olLanguage = "ita"; break;
                    case "ca": olLanguage = "cat"; break;
                }
            }
            var url = $"https://openlibrary.org/search.json?q={Uri.EscapeDataString(query)}";
            if (!string.IsNullOrWhiteSpace(olLanguage))
                url += $"&language={Uri.EscapeDataString(olLanguage)}";
            var result = new List<BookDto>();
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("[OpenLibrary] Respuesta no exitosa: {StatusCode}", response.StatusCode);
                    return result;
                }
                var json = await response.Content.ReadAsStringAsync();
                var root = JsonDocument.Parse(json).RootElement;
                if (root.TryGetProperty("docs", out var docs) && docs.ValueKind == JsonValueKind.Array)
                {
                    if (docs.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("[OpenLibrary] docs está vacío para la búsqueda actual.");
                    }
                    foreach (var doc in docs.EnumerateArray())
                    {
                        byte[]? coverBytes = null;
                        if (doc.TryGetProperty("cover_i", out var coverId) && coverId.GetInt32() > 0)
                        {
                            var coverUrl = $"https://covers.openlibrary.org/b/id/{coverId.GetInt32()}-L.jpg";
                            string logTitle = doc.TryGetProperty("title", out var tempTitle) ? tempTitle.GetString() ?? "Sin título" : "Sin título";
                            try
                            {
                                var imgResponse = await _httpClient.GetAsync(coverUrl);
                                if (imgResponse.IsSuccessStatusCode)
                                {
                                    coverBytes = await imgResponse.Content.ReadAsByteArrayAsync();
                                    _logger.LogInformation("✅ Portada OpenLibrary descargada para '{Title}' ({CoverUrl}), bytes: {Length}",
                                        logTitle, coverUrl, coverBytes?.Length ?? 0);
                                }
                                else
                                {
                                    _logger.LogWarning("⚠️ No se pudo descargar la portada OpenLibrary para '{Title}' ({CoverUrl})", 
                                        logTitle, coverUrl);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning("❌ Error al descargar portada OpenLibrary para '{Title}' ({CoverUrl}): {Message}",
                                    logTitle, coverUrl, ex.Message);
                            }
                        }
                        var book = new BookDto
                        {
                            Title = doc.TryGetProperty("title", out var t) ? t.GetString() ?? "Sin título" : "Sin título",
                            Author = doc.TryGetProperty("author_name", out var a) && a.ValueKind == JsonValueKind.Array ? string.Join(", ", a.EnumerateArray().Select(x => x.GetString())) : "Sin autor",
                            ISBN = doc.TryGetProperty("isbn", out var i) && i.ValueKind == JsonValueKind.Array ? i[0].GetString() : null,
                            Publisher = doc.TryGetProperty("publisher", out var p) && p.ValueKind == JsonValueKind.Array ? p[0].GetString() : null,
                            Summary = doc.TryGetProperty("first_sentence", out var s) ? s.GetString() : null,
                            Language = doc.TryGetProperty("language", out var l) && l.ValueKind == JsonValueKind.Array ? string.Join(", ", l.EnumerateArray().Select(x => x.GetString())) : null,
                            PageCount = doc.TryGetProperty("number_of_pages_median", out var pc) ? pc.GetInt32() : (int?)null,
                            PublicationDate = doc.TryGetProperty("publish_date", out var pd) && pd.ValueKind == JsonValueKind.Array && DateTime.TryParse(pd[0].GetString(), out var date) ? date : (DateTime?)null,
                            CoverImage = coverBytes
                        };
                        result.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("❌ Error al consultar OpenLibrary: {Message}", ex.Message);
            }
            return result;
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
                // 🧠 Obtener imagen de portada
                byte[]? coverBytes = null;
                if (root.TryGetProperty("covers", out var coversArray) && coversArray.ValueKind == JsonValueKind.Array && coversArray.GetArrayLength() > 0)
                {
                    var coverId = coversArray[0].GetInt32();
                    var coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-L.jpg";
                    try
                    {
                        var imgResponse = await _httpClient.GetAsync(coverUrl);
                        if (imgResponse.IsSuccessStatusCode)
                        {
                            coverBytes = await imgResponse.Content.ReadAsByteArrayAsync();
                            _logger.LogInformation("✅ Imagen de portada descargada correctamente para ISBN {Isbn}: {CoverUrl}", isbn, coverUrl);
                        }
                        else
                        {
                            _logger.LogWarning("⚠️ No se pudo descargar la imagen de portada para ISBN {Isbn}: {CoverUrl}", isbn, coverUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("❌ Error al descargar la imagen de portada para ISBN {Isbn}: {Message}", isbn, ex.Message);
                    }
                }
                // Asignación segura para evitar CS8601
                string safeTitle = root.TryGetProperty("title", out var title) ? (title.GetString() ?? string.Empty) : string.Empty;
                return new BookDto
                {
                    ISBN = isbn,
                    Title = safeTitle,
                    Author = !string.IsNullOrEmpty(authorNames) ? authorNames : string.Empty,
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
                    CoverImage = coverBytes // Aquí podrías descargar la imagen en el frontend y enviarla al backend
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
