using System.Threading;
using System.Threading.Tasks;

namespace Gnoss.Web.OAuth.Application.UseCases.VerifyOAuthRequest
{
    public interface IVerifyOAuthRequestUseCase
    {
        Task<VerifyOAuthRequestResult> ExecuteAsync(string pUrl, string httpMethod, CancellationToken ct = default);
    }
}
