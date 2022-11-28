
using Blazored.LocalStorage;
using Kiota.Builder.SearchProviders.GitHub.Authentication;

namespace Kiota.Web.Authentication.GitHub;

public class LocalStorageTokenStorageService : ITokenStorageService {
    private const string PATKey = "github-pat";
    public required ILocalStorageService LocalStorageService { get; init; }
    public async Task<string> GetTokenAsync(CancellationToken cancellationToken) {
        return await LocalStorageService.GetItemAsync<string>(PATKey, cancellationToken).ConfigureAwait(false);
    }
    public async Task SetTokenAsync(string value, CancellationToken cancellationToken) {
        await LocalStorageService.SetItemAsync(PATKey, value, cancellationToken).ConfigureAwait(false);
    }
    public async Task<bool> IsTokenPresentAsync(CancellationToken cancellationToken) {
        return !string.IsNullOrEmpty(await GetTokenAsync(cancellationToken).ConfigureAwait(false));
    }
    public async Task<bool> DeleteTokenAsync(CancellationToken cancellationToken) {
        await LocalStorageService.RemoveItemAsync(PATKey, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
