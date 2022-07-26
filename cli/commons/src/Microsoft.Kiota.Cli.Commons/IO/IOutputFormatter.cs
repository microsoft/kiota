namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// Output formatter contract.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Format and write stream content
    /// </summary>
    /// <param name="content">The stream content to format and write out</param>
    /// <param name="options">The options to use when formatting output</param>
    /// <param name="cancellationToken">The cancellation token</param>
    Task WriteOutputAsync(Stream content, IOutputFormatterOptions? options = null, CancellationToken cancellationToken = default);
}
