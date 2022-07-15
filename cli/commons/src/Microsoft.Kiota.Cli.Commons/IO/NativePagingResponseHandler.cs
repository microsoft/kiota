using Microsoft.Kiota.Abstractions;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Output filter contract. Implement this to provide output filtering capabilities to the CLI.
/// </summary>
public class NativePagingResponseHandler : NativeResponseHandler, IPagingResponseHandler
{
    /// <summary>
    /// Extract response content headers from a response handler.
    /// </summary>
    /// <returns>The response content headers.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response handler's <code>HandleResponseAsync</code> function hasn't yet run.</exception>
    public IDictionary<string, IEnumerable<string>> GetResponseContentHeaders()
    {
        if (Value is HttpResponseMessage responseMessage)
        {
            return new Dictionary<string, IEnumerable<string>>(responseMessage.Content.Headers, StringComparer.OrdinalIgnoreCase);
        }

        throw new InvalidOperationException("The response handler has not been invoked yet.");
    }

    /// <inheritdoc />
    public IDictionary<string, IEnumerable<string>> GetResponseHeaders()
    {
        if (Value is HttpResponseMessage responseMessage)
        {
            return new Dictionary<string, IEnumerable<string>>(responseMessage.Headers, StringComparer.OrdinalIgnoreCase);
        }

        throw new InvalidOperationException("The response handler has not been invoked yet.");
    }

    /// <inheritdoc />
    public async Task<Stream> GetResponseStreamAsync(CancellationToken cancellationToken = default)
    {
        if (Value is HttpResponseMessage responseMessage)
        {
            return await responseMessage.Content.ReadAsStreamAsync(cancellationToken);
        }

        throw new InvalidOperationException("The response handler has not been invoked yet.");
    }

    /// <inheritdoc />
    public int? GetStatusCode()
    {
        if (Value is HttpResponseMessage responseMessage)
        {
            return (int)responseMessage.StatusCode;
        }

        throw new InvalidOperationException("The response handler has not been invoked yet.");
    }
}
