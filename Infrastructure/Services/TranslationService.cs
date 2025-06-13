using Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Infrastructure.Services
{
    public class TranslationService : ITranslationService
    {
        private readonly IDistributedCache _cache;

        public TranslationService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<string> TranslateAsync(string key, string languageCode)
        {
            string cacheKey = $"translation:{languageCode}:{key}";
            var cached = await _cache.GetStringAsync(cacheKey);

            if (cached != null) return cached;

            // Simulación de traducción
            var translation = $"[{languageCode.ToUpper()}] {key}";
            await _cache.SetStringAsync(cacheKey, translation);
            return translation;
        }
    }
}

