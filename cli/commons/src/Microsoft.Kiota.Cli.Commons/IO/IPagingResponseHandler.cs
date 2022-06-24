using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Paging response handler contract.
/// </summary>
public interface IPagingResponseHandler : IResponseHandler
{
    /// <summary>
    /// Get response content headers.
    /// </summary>
    /// <returns>The response content headers.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response handler's <code>HandleResponseAsync</code> function hasn't yet run.</exception>
    IDictionary<string, IEnumerable<string>> GetResponseContentHeaders();

    /// <summary>
    /// Get response headers.
    /// </summary>
    /// <returns>The response headers.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response handler's <code>HandleResponseAsync</code> function hasn't yet run.</exception>
    IDictionary<string, IEnumerable<string>> GetResponseHeaders();

    /// <summary>
    /// Get a response stream.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The response content stream.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response handler's <code>HandleResponseAsync</code> function hasn't yet run.</exception>
    Task<Stream> GetResponseStreamAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status code.
    /// </summary>
    /// <returns>The status code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response handler's <code>HandleResponseAsync</code> function hasn't yet run.</exception>
    int? GetStatusCode();

}
