using Gnoss.Web.OAuth.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Infrastructure.Cache
{
    public sealed class RedisNonceStore(IConnectionMultiplexer redis, ILogger<RedisNonceStore> logger) : INonceStore
    {
        public async Task<bool> TryRegisterAsync(string consumerKey, string oauthToken, string nonce, TimeSpan ttl)
        {
            var key = $"oauth:nonce:{consumerKey}:{oauthToken}:{nonce}";

            try
            {
                return await redis.GetDatabase().StringSetAsync(key, 1, ttl, When.NotExists);
            }
            catch (RedisException ex)
            {
                logger.LogError(ex, "Redis no disponible al verificar nonce. Petición rechazada.");
                return false;
            }
        }
    }
}
