using System;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Domain.Interfaces
{
    public interface INonceStore
    {
        Task<bool> TryRegisterAsync(string consumerKey, string oauthToken, string nonce, TimeSpan ttl);
    }
}
