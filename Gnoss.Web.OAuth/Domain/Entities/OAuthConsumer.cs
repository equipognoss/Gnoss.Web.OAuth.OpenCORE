namespace Gnoss.Web.OAuth.Domain.Entities
{
    public sealed class OAuthConsumer
    {
        public int ConsumerId { get; init; }
        public string ConsumerKey { get; init; } = default!;
        public string ConsumerSecret { get; init; } = default!;
    }
}
