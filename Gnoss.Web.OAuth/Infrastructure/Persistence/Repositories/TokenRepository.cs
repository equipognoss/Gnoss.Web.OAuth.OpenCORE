using Gnoss.Web.OAuth.Application.Settings;
using Gnoss.Web.OAuth.Domain.Entities;
using Gnoss.Web.OAuth.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Infrastructure.Persistence.Repositories
{
    public sealed class TokenRepository(IDbContextFactory<AppDbContext> dbFactory, IConnectionMultiplexer redis, IOptions<OAuthSettings> options, ILogger<TokenRepository> logger) : ITokenRepository
    {
        private sealed record TokenCacheEntry(string TokenSecret, Guid? UsuarioID);

        public async Task<OAuthToken?> GetActiveTokenAsync(string token, CancellationToken ct = default)
        {
            var cacheKey = $"oauth:token:{token}";
            var ttl = TimeSpan.FromMinutes(options.Value.TokenCacheMinutes);

            try
            {
                var cached = await redis.GetDatabase().StringGetAsync(cacheKey);
                if (cached.HasValue)
                {
                    var entry = JsonSerializer.Deserialize<TokenCacheEntry>((string)cached!);
                    if (entry is not null)
                        return new OAuthToken { Token = token, TokenSecret = entry.TokenSecret, UsuarioID = entry.UsuarioID, State = 1 };
                }
            }
            catch (RedisException ex)
            {
                logger.LogWarning(ex, "Redis no disponible al leer caché de token. Consultando BD.");
            }

            await using var ctx = await dbFactory.CreateDbContextAsync(ct);

            var row = await ctx.OAuthTokens
                .AsNoTracking()
                .Where(t => t.Token == token /*&& t.State == 1*/)
                .FirstOrDefaultAsync(ct);

            if (row is null)
                return null;

            try
            {
                var entry = new TokenCacheEntry(row.TokenSecret, row.UsuarioID);
                await redis.GetDatabase().StringSetAsync(cacheKey, JsonSerializer.Serialize(entry), ttl);
            }
            catch (RedisException) { }

            return row;
        }
    }

}
