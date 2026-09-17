using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Gnoss.Web.OAuth.Domain.Services
{
    public static class OAuthSignatureService
    {
        private static readonly string[] ExcludeFromSignature = ["oauth_signature", "realm"];
        private static readonly string UnreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

        public static bool TryParseOAuthParams(
            string rawUrl,
            out string baseUrl,
            out Dictionary<string, string> oauthParams,
            out List<KeyValuePair<string, string>> allParams)
        {
            baseUrl = string.Empty;
            oauthParams = [];
            allParams = [];

            if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
                return false;

            // RFC 5849 §3.4.1.2: los puertos estándar (80/443) NO deben incluirse
            var isStandardPort = (uri.Scheme == "http" && uri.Port == 80)
                              || (uri.Scheme == "https" && uri.Port == 443)
                              || uri.Port == -1;
            baseUrl = isStandardPort
                ? $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}"
                : $"{uri.Scheme}://{uri.Host}:{uri.Port}{uri.AbsolutePath}";

            var query = uri.Query.TrimStart('?');
            if (string.IsNullOrEmpty(query))
                return false;

            foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = pair.IndexOf('=');
                if (idx < 0) continue;

                var key = Uri.UnescapeDataString(pair[..idx]);
                var value = Uri.UnescapeDataString(pair[(idx + 1)..]);

                allParams.Add(new KeyValuePair<string, string>(key, value));

                if (key.StartsWith("oauth_", StringComparison.Ordinal))
                    oauthParams[key] = value;
            }

            return oauthParams.Count > 0;
        }

        public static bool ValidateTimestamp(string timestampStr, int toleranceSeconds)
        {
            if (!long.TryParse(timestampStr, out var unixTs))
                return false;

            var diff = DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(unixTs);
            return diff.Duration() <= TimeSpan.FromSeconds(toleranceSeconds);
        }

        public static string BuildSignature(
            string httpMethod,
            string baseUrl,
            List<KeyValuePair<string, string>> allParams,
            string consumerSecret,
            string tokenSecret)
        {
            var normalizedParams = allParams
                .Where(p => !ExcludeFromSignature.Contains(p.Key, StringComparer.Ordinal))
                .OrderBy(p => PercentEncode(p.Key), StringComparer.Ordinal)
                .ThenBy(p => PercentEncode(p.Value), StringComparer.Ordinal)
                .Select(p => $"{PercentEncode(p.Key)}={PercentEncode(p.Value)}");

            var paramString = string.Join("&", normalizedParams);

            var signatureBaseString = string.Concat(
                httpMethod, "&",
                PercentEncode(baseUrl), "&",
                PercentEncode(paramString));

            var signingKey = $"{PercentEncode(consumerSecret)}&{PercentEncode(tokenSecret)}";

            var hash = HMACSHA1.HashData(
                Encoding.UTF8.GetBytes(signingKey),
                Encoding.UTF8.GetBytes(signatureBaseString));

            return Convert.ToBase64String(hash);
        }

        public static string PercentEncode(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var result = new StringBuilder();
            foreach (byte b in bytes)
            {
                char c = (char)b;
                if (UnreservedChars.IndexOf(c) != -1)
                    result.Append(c);
                else
                    result.Append('%').Append(b.ToString("X2"));
            }
            return result.ToString();
        }
    }
}
