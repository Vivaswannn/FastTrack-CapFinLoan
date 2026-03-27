namespace CapFinLoan.AdminService.Services.Interfaces
{
    /// <summary>
    /// Contract for caching operations.
    /// Abstracts Redis implementation so it can be mocked in tests
    /// and swapped for MemoryCache if Redis is unavailable.
    /// </summary>
    public interface ICacheService
    {
        /// <summary>Get value from cache by key</summary>
        Task<T?> GetAsync<T>(string key);

        /// <summary>Set value in cache with expiry</summary>
        Task SetAsync<T>(string key, T value, TimeSpan expiry);

        /// <summary>Remove value from cache</summary>
        Task RemoveAsync(string key);

        /// <summary>Remove all keys matching a pattern prefix</summary>
        Task RemoveByPrefixAsync(string prefix);
    }
}
