using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
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
    private const string UriEscapedCharacters = " \"<>^`{|}";

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
        var resolutionBase = !uri.IsAbsoluteUri && baseUrl is { IsAbsoluteUri: true } && OpenApiConstants.BaseRegistryUri.Equals(baseUrl.AbsoluteUri, StringComparison.Ordinal) ?
            new Uri(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar) : baseUrl;
        var targetUri = uri.IsAbsoluteUri || resolutionBase is null ? uri : new Uri(resolutionBase, uri);
        // Resolve file URIs the same way as DefaultStreamLoader, then authorize and open that one filesystem path.
        var localPath = targetUri.IsFile ? NormalizePathCandidate(new UriBuilder(targetUri) { Fragment = string.Empty }.Uri.LocalPath) : null;
        if (!(localPath is null ? IsAllowedUri(targetUri) : IsAllowedLocalPath(localPath)))
            throw new InvalidOperationException($"The external reference {targetUri} is not allowed. Add it to --allowed-external-origins to load it.");
        if (localPath is not null)
            return Task.FromResult<Stream>(File.OpenRead(localPath));
        if (IsHttpUri(targetUri) && (uri.IsAbsoluteUri || baseUrl is null || !OpenApiConstants.BaseRegistryUri.Equals(baseUrl.AbsoluteUri, StringComparison.Ordinal)))
            return LoadFromHttpAsync(targetUri, cancellationToken);
        return base.LoadAsync(baseUrl!, uri, cancellationToken);
    }

    // the original string can keep segments such as "..", which the request doesn't
    private static Uri Canonicalize(Uri uri) => new(uri.AbsoluteUri);

    private static bool IsHttpUri(Uri uri) =>
        uri.IsAbsoluteUri && IsHttpScheme(uri.Scheme);

    private static bool IsHttpScheme(string scheme) =>
        Uri.UriSchemeHttps.Equals(scheme, StringComparison.OrdinalIgnoreCase) || Uri.UriSchemeHttp.Equals(scheme, StringComparison.OrdinalIgnoreCase);

    // a scheme wildcard such as "*" or "http*" also matches http and https addresses, so the pattern is written the same way
    private static bool IsHttpOrWildcardScheme(string scheme) =>
        Matches(scheme, Uri.UriSchemeHttps) || Matches(scheme, Uri.UriSchemeHttp);

    private static bool IsRedirect(HttpStatusCode statusCode) => statusCode is
        HttpStatusCode.MultipleChoices or
        HttpStatusCode.MovedPermanently or
        HttpStatusCode.Found or
        HttpStatusCode.SeeOther or
        HttpStatusCode.TemporaryRedirect or
        HttpStatusCode.PermanentRedirect;

    private async Task<Stream> LoadFromHttpAsync(Uri targetUri, CancellationToken cancellationToken)
    {
        // the timeout covers the whole redirect chain, like it does when the client follows redirects
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (httpClient.Timeout != Timeout.InfiniteTimeSpan)
            timeoutSource.CancelAfter(httpClient.Timeout);
        try
        {
            return await FollowRedirectsAsync(targetUri, timeoutSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
        {
            throw new TaskCanceledException($"The external reference {targetUri} was not loaded within {httpClient.Timeout.TotalSeconds} seconds.", new TimeoutException(ex.Message, ex));
        }
    }

    private async Task<Stream> FollowRedirectsAsync(Uri targetUri, CancellationToken cancellationToken)
    {
        var requestUri = targetUri;
        for (var redirectCount = 0; ; redirectCount++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            try
            {
                if (response.RequestMessage?.RequestUri is { } respondingUri && respondingUri != requestUri && Canonicalize(respondingUri) is var canonicalRespondingUri && !IsAllowedUri(canonicalRespondingUri))
                    throw new InvalidOperationException($"The external reference {targetUri} was served from {respondingUri}, which is not allowed. Add it to --allowed-external-origins to load it.");
                if (!IsRedirect(response.StatusCode) || response.Headers.Location is not { } location)
                {
                    response.EnsureSuccessStatusCode();
                    // the body is read before returning so the timeout also covers it
                    var content = new MemoryStream();
                    try
                    {
                        await response.Content.CopyToAsync(content, cancellationToken).ConfigureAwait(false);
                    }
                    catch
                    {
                        await content.DisposeAsync().ConfigureAwait(false);
                        throw;
                    }
                    content.Position = 0;
                    response.Dispose();
                    return content;
                }
                var redirectUri = location.IsAbsoluteUri ? location : new Uri(requestUri, location);
                // same as the default handler: never follow a redirect to another scheme or from https to http
                if (!IsHttpUri(redirectUri) || (Uri.UriSchemeHttps.Equals(requestUri.Scheme, StringComparison.OrdinalIgnoreCase) && !Uri.UriSchemeHttps.Equals(redirectUri.Scheme, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidOperationException($"The external reference {targetUri} redirected to {redirectUri}, which is not supported.");
                redirectUri = Canonicalize(redirectUri);
                if (!IsAllowedUri(redirectUri))
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

    private bool IsAllowedLocalPath(string localPath)
    {
        return allowedExternalOrigins.Any(allowedOrigin =>
            allowedOrigin.Equals("*", StringComparison.Ordinal) ||
            Matches(NormalizeAllowedPath(allowedOrigin), localPath));
    }

    private bool IsAllowedUri(Uri targetUri)
    {
        if (allowedExternalOrigins.Count == 0)
            return false;

        var uriCandidates = GetUriCandidates(targetUri);
        return allowedExternalOrigins.Any(allowedOrigin =>
            allowedOrigin.Equals("*", StringComparison.Ordinal) ||
            MatchesAnyUriCandidate(allowedOrigin, uriCandidates));
    }

    private static IEnumerable<string> GetUriCandidates(Uri targetUri)
    {
        yield return targetUri.AbsoluteUri;
        // the absolute URI drops the default port, which a pattern with a port wildcard still expects
        if (IsHttpUri(targetUri) && targetUri.IsDefaultPort)
            yield return targetUri.GetComponents(UriComponents.AbsoluteUri | UriComponents.StrongPort, UriFormat.UriEscaped);
    }

    private static bool MatchesAnyUriCandidate(string pattern, IEnumerable<string> candidates)
    {
        var normalizedPattern = NormalizeUriPattern(pattern);
        if (!pattern.Contains('*', StringComparison.Ordinal))
        {
            // exact entries are written the way the resolved candidates are, e.g. "pet model.yaml" as "pet%20model.yaml"
            var canonicalPattern = Uri.TryCreate(pattern, UriKind.Absolute, out var patternUri) && IsHttpUri(patternUri) ? patternUri.AbsoluteUri : normalizedPattern;
            return candidates.Any(candidate =>
                pattern.Equals(candidate, StringComparison.OrdinalIgnoreCase) ||
                normalizedPattern.Equals(candidate, StringComparison.OrdinalIgnoreCase) ||
                canonicalPattern.Equals(candidate, StringComparison.OrdinalIgnoreCase));
        }

        var expression = BuildUriPatternExpression(pattern);
        var normalizedExpression = BuildUriPatternExpression(normalizedPattern);
        var escapedExpression = BuildUriPatternExpression(EscapeUriPattern(normalizedPattern));
        return candidates.Any(candidate =>
            Regex.IsMatch(candidate, expression, RegexOptions.IgnoreCase, RegexTimeout) ||
            Regex.IsMatch(candidate, normalizedExpression, RegexOptions.IgnoreCase, RegexTimeout) ||
            Regex.IsMatch(candidate, escapedExpression, RegexOptions.IgnoreCase, RegexTimeout));
    }

    /// <summary>
    /// Percent-encodes the characters <see cref="Uri.AbsoluteUri"/> encodes after the authority of an http, https or wildcard scheme pattern,
    /// e.g. "my schemas/*" as "my%20schemas/*", without resolving dot segments or touching the wildcards.
    /// </summary>
    private static string EscapeUriPattern(string pattern)
    {
        var schemeSeparatorIndex = pattern.IndexOf(SchemeSeparator, StringComparison.Ordinal);
        if (schemeSeparatorIndex < 0 || !IsHttpOrWildcardScheme(pattern[..schemeSeparatorIndex]))
            return pattern;
        var authorityEndIndex = pattern.IndexOfAny(AuthorityTerminators, schemeSeparatorIndex + SchemeSeparator.Length);
        if (authorityEndIndex < 0)
            return pattern;
        var builder = new StringBuilder(pattern[..authorityEndIndex]);
        Span<byte> utf8 = stackalloc byte[4];
        foreach (var rune in pattern[authorityEndIndex..].EnumerateRunes())
        {
            if (rune.Value > 0x7F || UriEscapedCharacters.Contains((char)rune.Value, StringComparison.Ordinal))
            {
                var length = rune.EncodeToUtf8(utf8);
                foreach (var b in utf8[..length])
                    builder.Append(CultureInfo.InvariantCulture, $"%{b:X2}");
            }
            else
                builder.Append((char)rune.Value);
        }
        return builder.ToString();
    }

    /// <summary>
    /// Writes an http, https or wildcard scheme pattern the way <see cref="Uri.AbsoluteUri"/> writes the addresses it matches:
    /// without the default port and with a slash after the authority.
    /// </summary>
    private static string NormalizeUriPattern(string pattern)
    {
        var schemeSeparatorIndex = pattern.IndexOf(SchemeSeparator, StringComparison.Ordinal);
        if (schemeSeparatorIndex < 0)
            return pattern;
        var scheme = pattern[..schemeSeparatorIndex];
        if (!IsHttpOrWildcardScheme(scheme))
            return pattern;
        var defaultPort = scheme.ToLowerInvariant() switch
        {
            "https" => ":443",
            "http" => ":80",
            _ => null,
        };
        var authorityStartIndex = schemeSeparatorIndex + SchemeSeparator.Length;
        var authorityEndIndex = pattern.IndexOfAny(AuthorityTerminators, authorityStartIndex);
        var authority = authorityEndIndex < 0 ? pattern[authorityStartIndex..] : pattern[authorityStartIndex..authorityEndIndex];
        var remainder = authorityEndIndex < 0 ? string.Empty : pattern[authorityEndIndex..];
        if (defaultPort is not null && authority.EndsWith(defaultPort, StringComparison.Ordinal))
            authority = authority[..^defaultPort.Length];
        if (!remainder.StartsWith('/'))
            remainder = $"/{remainder}";
        return $"{pattern[..authorityStartIndex]}{authority}{DecodeUnreservedCharacters(remainder)}";
    }

    [GeneratedRegex("%([0-9A-Fa-f]{2})", RegexOptions.None, 500)]
    private static partial Regex PercentEncodedCharacterRegex();

    /// <summary>
    /// Decodes the percent-encoded unreserved characters (letters, digits, "-", ".", "_" and "~") the same way
    /// <see cref="Uri.AbsoluteUri"/> does, e.g. "%73chemas" as "schemas", and keeps every other encoded character.
    /// </summary>
    private static string DecodeUnreservedCharacters(string value)
    {
        return PercentEncodedCharacterRegex().Replace(value, static match =>
        {
            var character = (char)Convert.ToByte(match.Groups[1].Value, 16);
            return char.IsAsciiLetterOrDigit(character) || character is '-' or '.' or '_' or '~' ? character.ToString() : match.Value;
        });
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
        if (Uri.TryCreate(allowedOrigin, UriKind.Absolute, out var uri))
        {
            if (!uri.IsFile)
                return allowedOrigin;
            if (!Path.IsPathRooted(allowedOrigin))
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
