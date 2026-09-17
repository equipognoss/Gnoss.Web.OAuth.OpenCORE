using System;

namespace Gnoss.Web.OAuth.Application.UseCases.VerifyOAuthRequest
{
    public enum VerifyOAuthStatus { Success, UrlTooLong, Unauthorized }

    public sealed class VerifyOAuthRequestResult
    {
        public static readonly VerifyOAuthRequestResult UrlTooLong = new(VerifyOAuthStatus.UrlTooLong);
        public static readonly VerifyOAuthRequestResult Unauthorized = new(VerifyOAuthStatus.Unauthorized);

        public static VerifyOAuthRequestResult Success(Guid usuarioId) =>
            new(VerifyOAuthStatus.Success) { UsuarioID = usuarioId };

        private VerifyOAuthRequestResult(VerifyOAuthStatus status) => Status = status;

        public VerifyOAuthStatus Status { get; }
        public Guid? UsuarioID { get; private init; }
    }
}
