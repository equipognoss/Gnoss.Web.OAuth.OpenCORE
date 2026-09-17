using Gnoss.Web.OAuth.Application.Settings;
using Gnoss.Web.OAuth.Domain.Interfaces;
using Gnoss.Web.OAuth.Domain.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Application.UseCases.VerifyOAuthRequest
{
    public sealed class VerifyOAuthRequestUseCase(ITokenRepository tokenRepository,IConsumerRepository consumerRepository,INonceStore nonceStore,IOptions<OAuthSettings> options,ILogger<VerifyOAuthRequestUseCase> logger) : IVerifyOAuthRequestUseCase
    {
        private OAuthSettings Settings => options.Value;

        public async Task<VerifyOAuthRequestResult> ExecuteAsync(string pUrl, string httpMethod, CancellationToken ct = default)
        {
            if (pUrl.Length > Settings.MaxUrlLength)
                return VerifyOAuthRequestResult.UrlTooLong;

            if (!OAuthSignatureService.TryParseOAuthParams(pUrl, out var baseUrl, out var oauthParams, out var allParams))
            {
                logger.LogWarning("No se pudieron parsear los parámetros OAuth de la URL.");
                return VerifyOAuthRequestResult.Unauthorized;
            }

            if (!oauthParams.TryGetValue("oauth_token", out var oauthToken) ||
                !oauthParams.TryGetValue("oauth_consumer_key", out var consumerKey) ||
                !oauthParams.TryGetValue("oauth_signature", out var receivedSig) ||
                !oauthParams.TryGetValue("oauth_timestamp", out var timestampStr) ||
                !oauthParams.TryGetValue("oauth_nonce", out var nonce))
            {
                logger.LogWarning("Faltan parámetros OAuth obligatorios.");
                return VerifyOAuthRequestResult.Unauthorized;
            }

            if (!OAuthSignatureService.ValidateTimestamp(timestampStr, Settings.TimestampToleranceSeconds))
            {
                logger.LogWarning("Timestamp OAuth fuera de la ventana permitida: {Timestamp}", timestampStr);
                return VerifyOAuthRequestResult.Unauthorized;
            }

            if (!await nonceStore.TryRegisterAsync(consumerKey, oauthToken, nonce, TimeSpan.FromSeconds(Settings.TimestampToleranceSeconds * 2)))
            {
                logger.LogWarning("Nonce OAuth ya utilizado: {Nonce}", nonce);
                return VerifyOAuthRequestResult.Unauthorized;
            }

            var tokenTask = tokenRepository.GetActiveTokenAsync(oauthToken, ct);
            var consumerTask = consumerRepository.GetConsumerSecretAsync(consumerKey, ct);
            await Task.WhenAll(tokenTask, consumerTask);

            var token = tokenTask.Result;
            var consumerSecret = consumerTask.Result;

            if (token is null)
            {
                logger.LogWarning("Token OAuth no encontrado o inactivo: {Token}", oauthToken);
                return VerifyOAuthRequestResult.Unauthorized;
            }

            if (consumerSecret is null)
            {
                logger.LogWarning("Consumer no encontrado: {ConsumerKey}", consumerKey);
                return VerifyOAuthRequestResult.Unauthorized;
            }

            var expectedSig = OAuthSignatureService.BuildSignature(
                httpMethod.ToUpperInvariant(), baseUrl, allParams, consumerSecret, token.TokenSecret);

            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.UTF8.GetBytes(Uri.UnescapeDataString(receivedSig)),
                    Encoding.UTF8.GetBytes(expectedSig)))
            {
                logger.LogWarning("Firma OAuth inválida para el token: {Token} con los parámetros metodo HTTP: {HttpMethod} baseUrl: {baseUrl}", oauthToken, httpMethod.ToUpperInvariant(), baseUrl);
                return VerifyOAuthRequestResult.Unauthorized;
            }

            return token.UsuarioID.HasValue
                ? VerifyOAuthRequestResult.Success(token.UsuarioID.Value)
                : VerifyOAuthRequestResult.Unauthorized;
        }
    }
}
