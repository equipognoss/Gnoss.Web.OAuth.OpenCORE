using Gnoss.Web.OAuth.Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Domain.Interfaces
{
    public interface ITokenRepository
    {
        Task<OAuthToken?> GetActiveTokenAsync(string token, CancellationToken ct = default);
    }
}
