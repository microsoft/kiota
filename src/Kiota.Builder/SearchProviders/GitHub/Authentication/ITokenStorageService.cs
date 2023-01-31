using System.Threading;
using System.Threading.Tasks;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

public interface ITokenStorageService
{
    Task<string?> GetTokenAsync(CancellationToken cancellationToken);
    Task SetTokenAsync(string value, CancellationToken cancellationToken);
    Task<bool> IsTokenPresentAsync(CancellationToken cancellationToken);
    Task<bool> DeleteTokenAsync(CancellationToken cancellationToken);
}
