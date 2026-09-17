using System.Threading;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Domain.Interfaces
{
    public interface IConsumerRepository
    {
        Task<string?> GetConsumerSecretAsync(string consumerKey, CancellationToken ct = default);
    }
}
