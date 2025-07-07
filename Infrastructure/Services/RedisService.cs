using Application.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using IDatabase = StackExchange.Redis.IDatabase;

namespace Infrastructure.Services
{
    public class RedisService : IRedisService
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisService> _logger;

        // Constructor que recibe una instancia de IConnectionMultiplexer para conectarse a Redis.
        // También recibe un ILogger para registrar errores.
        public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task SetTokenAsync(string key, string value, TimeSpan expiration)
        {
            try
            {
                _logger.LogInformation("Guardando en Redis: {Key} = {Value} por {expiration}", key, value, expiration);

                await _db.StringSetAsync(key, value, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando token en Redis con la clave {Key}", key);
            }
        }

        public async Task<string?> GetTokenAsync(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue ? value.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo token de Redis con la clave {Key}", key);
                return null;
            }
        }

        public async Task DeleteTokenAsync(string key)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando token de Redis con la clave {Key}", key);
            }
        }
        public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
        {
            try
            {
                await _db.StringSetAsync(key, value, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error guardando valor en Redis con la clave {Key}", key);
            }
        }

        public async Task<string?> GetAsync(string key)
        {
            try
            {
                var result = await _db.StringGetAsync(key);
                return result.HasValue ? result.ToString() : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accediendo a Redis con la clave {Key}", key);
                return null;
            }
        }
    }
}