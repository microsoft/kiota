
using Blazored.LocalStorage;

namespace Kiota.Web.Authentication.GitHub;

public class LocalStoragePatService {
    private const string PATKey = "github-pat";
    public required ILocalStorageService LocalStorageService { get; init; }
    public async Task<string> GetPatAsync(CancellationToken cancellationToken) {
        return await LocalStorageService.GetItemAsync<string>(PATKey, cancellationToken).ConfigureAwait(false);
    }
    public async Task SetPatAsync(string value, CancellationToken cancellationToken) {
        await LocalStorageService.SetItemAsync(PATKey, value, cancellationToken).ConfigureAwait(false);
    }
    public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken) {
        return !string.IsNullOrEmpty(await GetPatAsync(cancellationToken).ConfigureAwait(false));
    }
    public async Task SignOutAsync(CancellationToken cancellationToken) {
        await LocalStorageService.RemoveItemAsync(PATKey, cancellationToken).ConfigureAwait(false);
    }
}
