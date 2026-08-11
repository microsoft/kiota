using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Kiota.Builder;

internal sealed partial class AllowedExternalOriginsStreamLoader : DefaultStreamLoader, IStreamLoader
{
    private readonly HashSet<string> allowedExternalOrigins;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

    public AllowedExternalOriginsStreamLoader(HttpClient httpClient, IEnumerable<string> allowedExternalOrigins) : base(httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(allowedExternalOrigins);
        this.allowedExternalOrigins = allowedExternalOrigins
            .Select(static x => x.TrimQuotes())
            .Where(static x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    async Task<Stream> IStreamLoader.LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken)
    {
        return await LoadAsync(baseUrl, uri, cancellationToken).ConfigureAwait(false);
    }

    public new Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var targetUri = uri.IsAbsoluteUri || baseUrl is null ? uri : new Uri(baseUrl, uri);
        if (!IsAllowed(targetUri, uri))
            throw new InvalidOperationException($"The external reference {targetUri} is not allowed. Add it to --allowed-external-origins to load it.");
        return base.LoadAsync(baseUrl!, uri, cancellationToken);
    }

    private bool IsAllowed(Uri targetUri, Uri originalUri)
    {
        if (allowedExternalOrigins.Count == 0)
            return false;

        var uriCandidates = GetUriCandidates(targetUri, originalUri);
        var pathCandidates = GetPathCandidates(targetUri, originalUri);
        return allowedExternalOrigins.Any(allowedOrigin =>
            allowedOrigin.Equals("*", StringComparison.Ordinal) ||
            MatchesAnyCandidate(allowedOrigin, uriCandidates) ||
            MatchesAnyCandidate(NormalizeAllowedPath(allowedOrigin), pathCandidates));
    }

    private static IEnumerable<string> GetUriCandidates(Uri targetUri, Uri originalUri)
    {
        yield return targetUri.AbsoluteUri;
        yield return targetUri.OriginalString;
        yield return originalUri.OriginalString;
    }

    private static IEnumerable<string> GetPathCandidates(Uri targetUri, Uri originalUri)
    {
        if (targetUri.IsFile)
            yield return NormalizePathCandidate(targetUri.LocalPath);
        if (!originalUri.IsAbsoluteUri || originalUri.IsFile)
            yield return NormalizePathCandidate(originalUri.IsAbsoluteUri ? originalUri.LocalPath : originalUri.OriginalString);
    }

    private static bool MatchesAnyCandidate(string pattern, IEnumerable<string> candidates)
    {
        return candidates.Any(candidate => Matches(pattern, candidate));
    }

    private static bool Matches(string pattern, string candidate)
    {
        return pattern.Contains('*', StringComparison.Ordinal) ?
            Regex.IsMatch(candidate, $"^{Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal)}$", RegexOptions.IgnoreCase, RegexTimeout) :
            pattern.Equals(candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeAllowedPath(string allowedOrigin)
    {
        if (Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var uri) && !uri.IsFile)
            return allowedOrigin;

        var path = allowedOrigin.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        if (!Path.IsPathRooted(path))
            path = Path.Combine(Directory.GetCurrentDirectory(), path);
        return NormalizePathCandidate(path);
    }

    private static string NormalizePathCandidate(string path)
    {
        return Path.GetFullPath(path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
    }
}
