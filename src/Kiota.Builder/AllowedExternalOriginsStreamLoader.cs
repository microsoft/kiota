using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    private const string SchemeSeparator = "://";
    // the delimiters that separate the scheme, the user information, the host and the port from the rest of the URI.
    private const string AuthorityWildcardExpression = "[^/\\\\@:?#]*";
    private static readonly char[] AuthorityTerminators = ['/', '?', '#'];

    private const int MaxRedirects = 10;
    private readonly HttpClient httpClient;

    /// <param name="httpClient">The client used to download external references. It must not follow redirects automatically, the loader follows them and validates every hop.</param>
    /// <param name="allowedExternalOrigins">The origins external references can be loaded from.</param>
    public AllowedExternalOriginsStreamLoader(HttpClient httpClient, IEnumerable<string> allowedExternalOrigins) : base(httpClient)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(allowedExternalOrigins);
        this.httpClient = httpClient;
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
        if (IsHttpUri(targetUri) && (uri.IsAbsoluteUri || baseUrl is null || !OpenApiConstants.BaseRegistryUri.Equals(baseUrl.AbsoluteUri, StringComparison.Ordinal)))
            return LoadFromHttpAsync(targetUri, cancellationToken);
        return base.LoadAsync(baseUrl!, uri, cancellationToken);
    }

    // the original string can keep segments such as "..", which the request doesn't
    private static Uri Canonicalize(Uri uri) => new(uri.AbsoluteUri);

    private static bool IsHttpUri(Uri uri) =>
        uri.IsAbsoluteUri && (Uri.UriSchemeHttps.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase) || Uri.UriSchemeHttp.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase));

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MultipleChoices or
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Found or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private async Task<Stream> LoadFromHttpAsync(Uri targetUri, CancellationToken cancellationToken)
    {
        var requestUri = targetUri;
        for (var redirectCount = 0; ; redirectCount++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            try
            {
                if (response.RequestMessage?.RequestUri is { } respondingUri && respondingUri != requestUri && Canonicalize(respondingUri) is var canonicalRespondingUri && !IsAllowed(canonicalRespondingUri, canonicalRespondingUri))
                    throw new InvalidOperationException($"The external reference {targetUri} was served from {respondingUri}, which is not allowed. Add it to --allowed-external-origins to load it.");
                if (!IsRedirect(response.StatusCode) || response.Headers.Location is not { } location)
                {
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                }
                var redirectUri = location.IsAbsoluteUri ? location : new Uri(requestUri, location);
                // same as the default handler: never follow a redirect to another scheme or from https to http
                if (!IsHttpUri(redirectUri) || (Uri.UriSchemeHttps.Equals(requestUri.Scheme, StringComparison.OrdinalIgnoreCase) && !Uri.UriSchemeHttps.Equals(redirectUri.Scheme, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"The external reference {targetUri} redirected to {redirectUri}, which is not supported.");
                redirectUri = Canonicalize(redirectUri);
                if (!IsAllowed(redirectUri, redirectUri))
                    throw new InvalidOperationException($"The external reference {targetUri} redirected to {redirectUri}, which is not allowed. Add it to --allowed-external-origins to load it.");
                if (redirectCount >= MaxRedirects)
                    throw new InvalidOperationException($"The external reference {targetUri} exceeded the maximum number of redirects ({MaxRedirects}).");
                requestUri = redirectUri;
            }
            catch
            {
                response.Dispose();
                throw;
            }
            response.Dispose();
        }
    }

    private bool IsAllowed(Uri targetUri, Uri originalUri)
    {
        if (allowedExternalOrigins.Count == 0)
            return false;

        var uriCandidates = GetUriCandidates(targetUri, originalUri);
        var pathCandidates = GetPathCandidates(targetUri, originalUri);
        return allowedExternalOrigins.Any(allowedOrigin =>
            allowedOrigin.Equals("*", StringComparison.Ordinal) ||
            MatchesAnyUriCandidate(allowedOrigin, uriCandidates) ||
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

    private static bool MatchesAnyUriCandidate(string pattern, IEnumerable<string> candidates)
    {
        if (!pattern.Contains('*', StringComparison.Ordinal))
            return candidates.Any(candidate => pattern.Equals(candidate, StringComparison.OrdinalIgnoreCase));

        var expression = BuildUriPatternExpression(pattern);
        return candidates.Any(candidate => Regex.IsMatch(candidate, expression, RegexOptions.IgnoreCase, RegexTimeout));
    }

    /// <summary>
    /// Builds the expression for a URI pattern so a wildcard placed before the path cannot consume the delimiters
    /// that end the authority. Expanding such a wildcard to ".*" lets it continue past the host and complete the
    /// match with text taken from the path, the user information or the port, which authorizes hosts outside the
    /// intended set.
    /// Wildcards from the path onwards keep matching any character since the destination is already pinned by then.
    /// </summary>
    private static string BuildUriPatternExpression(string pattern)
    {
        var schemeSeparatorIndex = pattern.IndexOf(SchemeSeparator, StringComparison.Ordinal);
        var authorityEndIndex = schemeSeparatorIndex < 0 ? -1 : pattern.IndexOfAny(AuthorityTerminators, schemeSeparatorIndex + SchemeSeparator.Length);
        var authority = authorityEndIndex < 0 ? pattern : pattern[..authorityEndIndex];
        var remainder = authorityEndIndex < 0 ? string.Empty : pattern[authorityEndIndex..];
        return $"^{Regex.Escape(authority).Replace("\\*", AuthorityWildcardExpression, StringComparison.Ordinal)}{Regex.Escape(remainder).Replace("\\*", ".*", StringComparison.Ordinal)}$";
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
