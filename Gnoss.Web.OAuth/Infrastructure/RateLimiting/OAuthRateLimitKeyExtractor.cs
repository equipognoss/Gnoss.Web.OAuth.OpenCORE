using Microsoft.AspNetCore.Http;
using System;

namespace Gnoss.Web.OAuth.Infrastructure.RateLimiting
{
    public static class OAuthRateLimitKeyExtractor
    {
        public static string Extract(HttpRequest request)
        {
            var pUrl = request.Query["pUrl"].ToString();

            if (!string.IsNullOrEmpty(pUrl))
            {
                var decoded = Uri.UnescapeDataString(pUrl);
                if (Uri.TryCreate(decoded, UriKind.Absolute, out var uri))
                {
                    var query = uri.Query.TrimStart('?');
                    foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var idx = pair.IndexOf('=');
                        if (idx > 0 && Uri.UnescapeDataString(pair[..idx]) == "oauth_token")
                            return $"token:{Uri.UnescapeDataString(pair[(idx + 1)..])}";
                    }
                }
            }

            return $"ip:{request.HttpContext.Connection.RemoteIpAddress}";
        }
    }
}
