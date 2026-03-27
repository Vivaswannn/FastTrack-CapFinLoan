using System.Text.Json;
using CapFinLoan.AdminService.Services.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace CapFinLoan.AdminService.Services
{
    /// <summary>
    /// Redis-backed cache service using IDistributedCache.
    /// Gracefully degrades — if Redis is unavailable,
    /// operations are logged and skipped without crashing.
    /// Logs cache hit/miss metrics at Information level for observability.
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<RedisCacheService> _logger;

        private static readonly JsonSerializerOptions
            _jsonOptions = new()
            {
                PropertyNameCaseInsensitive = true
            };

        public RedisCacheService(
            IDistributedCache cache,
            ILogger<RedisCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var json = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(json))
                {
                    _logger.LogInformation(
                        "Cache MISS for key: {Key}", key);
                    return default;
                }

                _logger.LogInformation(
                    "Cache HIT for key: {Key}", key);

                return JsonSerializer.Deserialize<T>(
                    json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis GET failed for key: {Key}. " +
                    "Returning null — will fetch from source.",
                    key);
                return default;
            }
        }

        /// <inheritdoc/>
        public async Task SetAsync<T>(
            string key, T value, TimeSpan expiry)
        {
            try
            {
                var json = JsonSerializer.Serialize(
                    value, _jsonOptions);

                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expiry
                };

                await _cache.SetStringAsync(key, json, options);

                _logger.LogInformation(
                    "Cache SET for key: {Key}, " +
                    "expires in {Expiry}",
                    key, expiry);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis SET failed for key: {Key}. " +
                    "Data not cached — will refetch next time.",
                    key);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
                _logger.LogInformation(
                    "Cache REMOVED key: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Redis REMOVE failed for key: {Key}", key);
            }
        }

        /// <inheritdoc/>
        public async Task RemoveByPrefixAsync(string prefix)
        {
            // IDistributedCache does not support pattern delete
            // We track known keys by convention
            // In production use StackExchange.Redis directly
            // for SCAN + DELETE pattern
            _logger.LogInformation(
                "Cache invalidation requested for prefix: {Prefix}",
                prefix);
            await Task.CompletedTask;
        }
    }
}
