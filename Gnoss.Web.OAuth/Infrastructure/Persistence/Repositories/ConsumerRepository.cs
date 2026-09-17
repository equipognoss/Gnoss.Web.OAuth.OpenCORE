using Gnoss.Web.OAuth.Application.Settings;
using Gnoss.Web.OAuth.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Infrastructure.Persistence.Repositories
{
    public sealed class ConsumerRepository(IDbContextFactory<AppDbContext> dbFactory, IConnectionMultiplexer redis, IOptions<OAuthSettings> options, ILogger<ConsumerRepository> logger) : IConsumerRepository
    {
        public async Task<string?> GetConsumerSecretAsync(string consumerKey, CancellationToken ct = default)
        {
            var cacheKey = $"oauth:consumer:{consumerKey}";
            var ttl = TimeSpan.FromMinutes(options.Value.ConsumerCacheMinutes);

            try
            {
                var cached = await redis.GetDatabase().StringGetAsync(cacheKey);
                if (cached.HasValue)
                    return cached.ToString();
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Redis no disponible al leer caché de consumer. Consultando BD.");
            }

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var secret = await ctx.OAuthConsumers
                .AsNoTracking()
                .Where(c => c.ConsumerKey == consumerKey)
                .Select(c => c.ConsumerSecret)
                .FirstOrDefaultAsync(ct);

            if (secret is null)
                return null;

            try
            {
                await redis.GetDatabase().StringSetAsync(cacheKey, secret, ttl);
            }
            catch (RedisException) { }

            return secret;
        }
    }
}
