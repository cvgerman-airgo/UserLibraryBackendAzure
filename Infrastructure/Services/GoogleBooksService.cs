using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class GoogleBooksService
    {
        private readonly HttpClient _httpClient;

        public GoogleBooksService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SearchAsync(
            string query, 
            string? language = null, 
            string? country = null)
        {
            var url = $"https://www.googleapis.com/books/v1/volumes?q={Uri.EscapeDataString(query)}";
            if (!string.IsNullOrWhiteSpace(language))
                url += $"&langRestrict={Uri.EscapeDataString(language)}";
            if (!string.IsNullOrWhiteSpace(country))
                url += $"&country={Uri.EscapeDataString(country)}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }

    }
}
