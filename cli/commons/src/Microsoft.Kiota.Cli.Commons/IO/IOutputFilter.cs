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
    /// <returns>A filtered stream</returns>
    Stream FilterOutput(Stream content, string query);

    /// <summary>
    /// Run a filter on string content based on a query. The query format is determined by the implementation
    /// </summary>
    /// <param name="content">String content to filter</param>
    /// <param name="query">Query to run against the content</param>
    /// <returns>A filtered string</returns>
    string FilterOutput(string content, string query);
}
