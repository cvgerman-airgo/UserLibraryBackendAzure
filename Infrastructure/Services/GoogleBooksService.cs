using System.Net.Http;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Application.DTOs;
using Application.Interfaces;
using Infrastructure.Extensions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class GoogleBooksService : IGoogleBooksService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<AuthService> _logger;

    public GoogleBooksService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<AuthService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["GoogleBooks:ApiKey"] ?? throw new InvalidOperationException("GoogleBooks API key is missing.");
        _logger = logger;
    }

    public async Task<BookDto?> SearchByIsbnAsync(string isbn)
    {
        var url = $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}&key={_apiKey}";
        var response = await _httpClient.GetAsync(url);
        //        _logger.LogInformation("Buscando libro por ISBN: {ISBN}", isbn);
        //        _logger.LogInformation("URL de búsqueda: {Url}", url);
        //        _logger.LogInformation("Respuesta del servicio: {StatusCode}", response.StatusCode);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        //        _logger.LogInformation("📖 JSON crudo: {json}", json);

        try
        {
            var googleData = JsonSerializer.Deserialize<GoogleBooksApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            //            _logger.LogInformation("✅ Deserialización: {googleData}", JsonSerializer.Serialize(googleData));
            //            _logger.LogInformation("✅ Total items: {Count}", googleData?.Items?.Count ?? 0);


            var item = googleData?.Items?.FirstOrDefault();
            if (item?.VolumeInfo == null)
            {
                _logger.LogWarning("⚠️ No se encontró VolumeInfo en Items");
            }

            return item?.ToBookDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error deserializando JSON de Google Books");
            throw;
        }
    }

    public async Task<List<BookDto>> SearchGoogleBooksAsync(string? title, string? author, string? language)
    {
        // Construimos la query según los parámetros no nulos
        var queryParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(title))
            queryParts.Add($"intitle:{title}");
        if (!string.IsNullOrWhiteSpace(author))
            queryParts.Add($"inauthor:{author}");

        var query = string.Join("+", queryParts);
        try
        {
            var url = $"https://www.googleapis.com/books/v1/volumes?q={Uri.EscapeDataString(query)}";
            if (!string.IsNullOrWhiteSpace(language))
                url += $"&langRestrict={Uri.EscapeDataString(language)}";

            url += $"&key={_apiKey}";

//            _logger.LogInformation("📚 GoogleBooks query: {Query}", query);
//            _logger.LogInformation("🌐 Request URL: {Url}", url);

            var response = await _httpClient.GetAsync(url);

//            _logger.LogInformation("📡 Response status code: {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("⚠️ GoogleBooks API returned error status");
                return new List<BookDto>();
            }

            var json = await response.Content.ReadAsStringAsync();

//            _logger.LogInformation("📖 JSON response: {Json}", json);

            var googleData = JsonSerializer.Deserialize<GoogleBooksApiResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (googleData?.Items == null || googleData.Items.Count == 0)
            {
                _logger.LogInformation("ℹ️ No books found in GoogleBooks response.");
                return new List<BookDto>();
            }

            return googleData.Items
                .Select(item => item.ToBookDto())
                .Where(dto => dto != null && !string.IsNullOrWhiteSpace(dto.ISBN))
                .ToList()!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Exception searching books in GoogleBooks with query: {Query}", query);
            return new List<BookDto>();
        }
    }

}

