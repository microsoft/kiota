namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Output filter contract. Implement this to provide output filtering capabilities to the CLI.
/// </summary>
public interface IOutputFilter
{
    /// <summary>
    /// Run a filter on stream content based on a query. The query format is determined by the implementation
    /// </summary>
    /// <param name="content">Stream content to filter</param>
    /// <param name="query">Query to run against the content</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A filtered stream</returns>
    Task<Stream> FilterOutputAsync(Stream content, string query, CancellationToken cancellationToken = default);
}
