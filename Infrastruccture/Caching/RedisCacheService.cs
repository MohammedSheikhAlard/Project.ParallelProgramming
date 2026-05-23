using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace Infrastructure.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public RedisCacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken token = default)
        {
            var cachedBytes = await _cache.GetAsync(key, token);
            if (cachedBytes is null) return default;
            return JsonSerializer.Deserialize<T>(cachedBytes);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken token = default)
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };
            await _cache.SetAsync(key, bytes, options, token);
        }

        public async Task RemoveAsync(string key, CancellationToken token = default)
        {
            await _cache.RemoveAsync(key, token);
        }
    }
}
