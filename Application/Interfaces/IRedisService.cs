
namespace Application.Interfaces
{
    public interface IRedisService
    {
        Task SetTokenAsync(string key, string value, TimeSpan expiration);
        Task<string?> GetTokenAsync(string key);
        Task DeleteTokenAsync(string key);
        Task SetAsync(string key, string value, TimeSpan? expiry = null);
        Task<string?> GetAsync(string key);
    }

}
