
using Blazored.SessionStorage;

namespace Kiota.Web.Authentication.GitHub;

public class SessionStoragePatService {
    private const string PATKey = "github-pat";
    public required ISessionStorageService SessionStorageService { get; init; }
    public async Task<string> GetPatAsync(CancellationToken cancellationToken) {
        return await SessionStorageService.GetItemAsync<string>(PATKey, cancellationToken).ConfigureAwait(false);
    }
    public async Task SetPatAsync(string value, CancellationToken cancellationToken) {
        await SessionStorageService.SetItemAsync(PATKey, value, cancellationToken).ConfigureAwait(false);
    }
    public async Task<bool> IsSignedInAsync(CancellationToken cancellationToken) {
        return !string.IsNullOrEmpty(await GetPatAsync(cancellationToken).ConfigureAwait(false));
    }
    public async Task SignOutAsync(CancellationToken cancellationToken) {
        await SessionStorageService.RemoveItemAsync(PATKey, cancellationToken).ConfigureAwait(false);
    }
}
