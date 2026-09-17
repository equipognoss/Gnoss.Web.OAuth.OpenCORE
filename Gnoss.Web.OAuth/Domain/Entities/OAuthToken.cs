using System;

namespace Gnoss.Web.OAuth.Domain.Entities
{

    public sealed class OAuthToken
    {
        public int TokenId { get; init; }
        public string Token { get; init; } = default!;
        public string TokenSecret { get; init; } = default!;
        public int State { get; init; }
        public int ConsumerId { get; init; }
        public Guid? UsuarioID { get; init; }
    }
}
