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
    private readonly HttpClient httpClient;
    private readonly HashSet<string> allowedExternalOrigins;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);
    private const int MaxRedirectCount = 10;
    private const string SchemeSeparator = "://";
    // the delimiters that separate the scheme, the user information, the host and the port from the rest of the URI.
    private const string AuthorityWildcardExpression = "[^/\\\\@:?#]*";
    private static readonly char[] AuthorityTerminators = ['/', '?', '#'];

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

    public new async Task<Stream> LoadAsync(Uri baseUrl, Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        var targetUri = uri.IsAbsoluteUri || baseUrl is null ? uri : new Uri(baseUrl, uri);
        if (!IsAllowed(targetUri, uri))
            throw new InvalidOperationException($"The external reference {targetUri} is not allowed. Add it to --allowed-external-origins to load it.");
        if (!IsHttpUri(targetUri))
            return await base.LoadAsync(baseUrl!, uri, cancellationToken).ConfigureAwait(false);

        return await LoadHttpAsync(targetUri, cancellationToken).ConfigureAwait(false);
    }

    private async Task<Stream> LoadHttpAsync(Uri targetUri, CancellationToken cancellationToken)
    {
        var currentUri = targetUri;
        for (var redirectCount = 0; ; redirectCount++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            if (IsRedirect(response.StatusCode))
            {
                var location = response.Headers.Location;
                response.Dispose();
                if (location is null)
                    throw new HttpRequestException($"The external reference {currentUri} returned a redirect without a Location header.");
                if (redirectCount >= MaxRedirectCount)
                    throw new HttpRequestException($"The external reference {targetUri} exceeded the maximum redirect count of {MaxRedirectCount}.");

                var redirectLocation = location.ToString();
                var redirectUri = Uri.TryCreate(redirectLocation, UriKind.Absolute, out var absoluteRedirectUri)
                    ? absoluteRedirectUri
                    : new Uri(currentUri, redirectLocation);
                if (!IsHttpUri(redirectUri) || !IsAllowed(redirectUri, redirectUri))
                    throw new InvalidOperationException($"The external reference redirect to {redirectUri} is not allowed. Add it to --allowed-external-origins to load it.");
                currentUri = redirectUri;
                continue;
            }

            try
            {
                response.EnsureSuccessStatusCode();
                var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                return new ResponseOwnedStream(contentStream, response);
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }
    }

    private static bool IsRedirect(System.Net.HttpStatusCode statusCode)
    {
        return statusCode is System.Net.HttpStatusCode.MovedPermanently or
            System.Net.HttpStatusCode.Redirect or
            System.Net.HttpStatusCode.SeeOther or
            System.Net.HttpStatusCode.TemporaryRedirect or
            System.Net.HttpStatusCode.PermanentRedirect;
    }

    private static bool IsHttpUri(Uri uri)
    {
        return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ResponseOwnedStream(Stream innerStream, HttpResponseMessage response) : Stream
    {
        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;
        public override long Length => innerStream.Length;
        public override long Position { get => innerStream.Position; set => innerStream.Position = value; }
        public override void Flush() => innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => innerStream.Read(buffer);
        public override long Seek(long offset, SeekOrigin origin) => innerStream.Seek(offset, origin);
        public override void SetLength(long value) => innerStream.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => innerStream.Write(buffer, offset, count);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) => innerStream.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) => innerStream.ReadAsync(buffer, offset, count, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    innerStream.Dispose();
                }
                finally
                {
                    response.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            try
            {
                await innerStream.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                response.Dispose();
                await base.DisposeAsync().ConfigureAwait(false);
            }
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
