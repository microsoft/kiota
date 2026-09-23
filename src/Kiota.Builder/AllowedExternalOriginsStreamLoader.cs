using System;
using System.Collections.Generic;
using System.Globalization;
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
    private const string SchemeSeparator = "://";

    private readonly record struct UriPattern(string Scheme, string? UserInfo, string Host, int? Port, string? Path);

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
        var absoluteUriCandidates = GetAbsoluteUriCandidates(targetUri, originalUri);
        var pathCandidates = GetPathCandidates(targetUri, originalUri);
        return allowedExternalOrigins.Any(allowedOrigin =>
            allowedOrigin.Equals("*", StringComparison.Ordinal) ||
            MatchesAnyUriCandidate(allowedOrigin, uriCandidates, absoluteUriCandidates) ||
            MatchesAnyPathCandidate(NormalizeAllowedPath(allowedOrigin), pathCandidates));
    }

    private static IEnumerable<string> GetUriCandidates(Uri targetUri, Uri originalUri)
    {
        yield return targetUri.AbsoluteUri;
        yield return targetUri.OriginalString;
        yield return originalUri.OriginalString;
    }

    private static IEnumerable<Uri> GetAbsoluteUriCandidates(Uri targetUri, Uri originalUri)
    {
        if (targetUri.IsAbsoluteUri)
            yield return targetUri;
        if (originalUri.IsAbsoluteUri && !ReferenceEquals(originalUri, targetUri))
            yield return originalUri;
    }

    private static IEnumerable<string> GetPathCandidates(Uri targetUri, Uri originalUri)
    {
        if (targetUri.IsFile)
            yield return NormalizePathCandidate(targetUri.LocalPath);
        if (!originalUri.IsAbsoluteUri || originalUri.IsFile)
            yield return NormalizePathCandidate(originalUri.IsAbsoluteUri ? originalUri.LocalPath : originalUri.OriginalString);
    }

    private static bool MatchesAnyUriCandidate(string pattern, IEnumerable<string> rawCandidates, IEnumerable<Uri> absoluteCandidates)
    {
        if (!pattern.Contains('*', StringComparison.Ordinal))
            return rawCandidates.Any(candidate => pattern.Equals(candidate, StringComparison.OrdinalIgnoreCase));

        // A wildcard URL pattern is matched component by component against the parsed URI instead of
        // against the URI string: matching the string lets the wildcard cross the authority boundary,
        // so an attacker-controlled host satisfies the pattern by replaying the allowed suffix in the
        // path, e.g. https://evil.example.com/x/.contoso.com/y matching https://*.contoso.com/*.
        return TryParseUriPattern(pattern, out var uriPattern) &&
            absoluteCandidates.Any(candidate => MatchesUriPattern(uriPattern, candidate));
    }

    private static bool MatchesAnyPathCandidate(string pattern, IEnumerable<string> candidates)
    {
        if (IsUriPattern(pattern))
            return false;
        return candidates.Any(candidate => MatchesGlob(pattern, candidate));
    }

    private static bool MatchesUriPattern(UriPattern pattern, Uri candidate)
    {
        if (!string.Equals(pattern.Scheme, candidate.Scheme, StringComparison.OrdinalIgnoreCase))
            return false;
        if (pattern.UserInfo is { } expectedUserInfo && !MatchesComponent(expectedUserInfo, candidate.UserInfo))
            return false;
        // the host is matched on its own so the wildcard cannot expand past the authority.
        if (!MatchesComponent(pattern.Host, candidate.Host))
            return false;
        if (pattern.Port is { } expectedPort ? expectedPort != candidate.Port : !candidate.IsDefaultPort)
            return false;
        return pattern.Path is not { } expectedPath ?
            candidate.AbsolutePath.Equals("/", StringComparison.Ordinal) :
            MatchesComponent(expectedPath, candidate.AbsolutePath);
    }

    private static bool MatchesComponent(string pattern, string value)
    {
        return pattern.Contains('*', StringComparison.Ordinal) ?
            MatchesGlob(pattern, value) :
            pattern.Equals(value, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesGlob(string pattern, string candidate)
    {
        return pattern.Contains('*', StringComparison.Ordinal) ?
            Regex.IsMatch(candidate, $"^{Regex.Escape(pattern).Replace("\\*", ".*", StringComparison.Ordinal)}$", RegexOptions.IgnoreCase, RegexTimeout) :
            pattern.Equals(candidate, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUriPattern(string pattern)
    {
        return pattern.Contains(SchemeSeparator, StringComparison.Ordinal);
    }

    private static bool TryParseUriPattern(string pattern, out UriPattern result)
    {
        result = default;
        var schemeSeparatorIndex = pattern.IndexOf(SchemeSeparator, StringComparison.Ordinal);
        if (schemeSeparatorIndex <= 0)
            return false;

        var scheme = pattern[..schemeSeparatorIndex];
        var remainder = pattern[(schemeSeparatorIndex + SchemeSeparator.Length)..];
        var pathSeparatorIndex = remainder.IndexOf('/', StringComparison.Ordinal);
        var authority = pathSeparatorIndex < 0 ? remainder : remainder[..pathSeparatorIndex];
        var path = pathSeparatorIndex < 0 ? null : remainder[pathSeparatorIndex..];
        var userInfoSeparatorIndex = authority.LastIndexOf('@');
        var userInfo = userInfoSeparatorIndex < 0 ? null : authority[..userInfoSeparatorIndex];
        var hostAndPort = userInfoSeparatorIndex < 0 ? authority : authority[(userInfoSeparatorIndex + 1)..];
        if (!TryParseAuthority(hostAndPort, out var host, out var port))
            return false;

        result = new UriPattern(scheme, userInfo, host, port, path);
        return true;
    }

    private static bool TryParseAuthority(string authority, out string host, out int? port)
    {
        host = string.Empty;
        port = null;
        if (string.IsNullOrEmpty(authority))
            return false;

        string portPart;
        if (authority[0] == '[')
        { // IPv6 literal, the port separator is the first colon after the closing bracket
            var closingBracketIndex = authority.IndexOf(']', StringComparison.Ordinal);
            if (closingBracketIndex < 0)
                return false;
            host = authority[..(closingBracketIndex + 1)];
            portPart = authority[(closingBracketIndex + 1)..];
        }
        else
        {
            var portSeparatorIndex = authority.LastIndexOf(':');
            host = portSeparatorIndex < 0 ? authority : authority[..portSeparatorIndex];
            portPart = portSeparatorIndex < 0 ? string.Empty : authority[portSeparatorIndex..];
        }

        if (string.IsNullOrEmpty(host))
            return false;
        if (portPart.Length == 0)
            return true;
        if (portPart[0] != ':' || !int.TryParse(portPart[1..], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPort))
            return false;

        port = parsedPort;
        return true;
    }

    private static string NormalizeAllowedPath(string allowedOrigin)
    {
        if (Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var uri))
        {
            if (!uri.IsFile)
                return allowedOrigin;
            allowedOrigin = uri.LocalPath;
        }

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
