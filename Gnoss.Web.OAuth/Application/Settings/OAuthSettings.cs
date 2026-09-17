using System.ComponentModel.DataAnnotations;

namespace Gnoss.Web.OAuth.Application.Settings
{
    public sealed class OAuthSettings
    {
        [Range(1, 86400)]
        public int TimestampToleranceSeconds { get; init; } = 300;

        [Range(1, int.MaxValue)]
        public int TokenCacheMinutes { get; init; } = 5;

        [Range(1, int.MaxValue)]
        public int ConsumerCacheMinutes { get; init; } = 60;

        [Range(100, int.MaxValue)]
        public int MaxUrlLength { get; init; } = 4096;

        [Range(1, int.MaxValue)]
        public int RateLimitPermitLimit { get; init; } = 600;

        [Range(1, int.MaxValue)]
        public int RateLimitWindowSeconds { get; init; } = 60;

        [Range(1, int.MaxValue)]
        public int RateLimitSegments { get; init; } = 6;
    }
}
