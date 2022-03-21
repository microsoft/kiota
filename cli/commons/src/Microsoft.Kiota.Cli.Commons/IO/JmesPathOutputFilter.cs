using System.Text;
using DevLab.JmesPath;

namespace Microsoft.Kiota.Cli.Commons.IO;

/// <summary>
/// An output filter that uses JMESPath queries to filter the output
/// </summary>
public class JmesPathOutputFilter : IOutputFilter
{
    private readonly JmesPath jmesPath;

    /// <summary>
    /// Creates a new instance of JmesPathOutputFilter
    /// </summary>
    /// <param name="jmesPath">The JmesPath transformer instance.</param>
    public JmesPathOutputFilter(JmesPath jmesPath)
    {
        if (jmesPath is null) throw new ArgumentNullException(nameof(jmesPath), $"Parameter '{nameof(jmesPath)}' is required.");
        this.jmesPath = jmesPath;
    }

    /// <inheritdoc />
    public async Task<Stream> FilterOutputAsync(Stream content, string query, CancellationToken cancellationToken = default) {
        if (string.IsNullOrEmpty(query)) return content;
        cancellationToken.ThrowIfCancellationRequested();
        using var reader = new StreamReader(content);
        var strContent = await reader.ReadToEndAsync();
        cancellationToken.ThrowIfCancellationRequested();
        var filtered = jmesPath.Transform(strContent, query);
        cancellationToken.ThrowIfCancellationRequested();
        var bytes = Encoding.UTF8.GetBytes(filtered);
        return new MemoryStream(bytes);
    }
}
