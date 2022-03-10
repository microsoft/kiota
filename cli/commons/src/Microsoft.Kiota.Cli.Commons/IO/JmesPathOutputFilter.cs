using System.Text;
using DevLab.JmesPath;

namespace Microsoft.Kiota.Cli.Commons.IO;

public class JmesPathOutputFilter : IOutputFilter
{
    private readonly JmesPath jmesPath;

    public JmesPathOutputFilter(JmesPath jmesPath)
    {
        if (jmesPath is null) throw new ArgumentNullException(nameof(jmesPath), $"Parameter '{nameof(jmesPath)}' is required.");
        this.jmesPath = jmesPath;
    }

    public Stream FilterOutput(Stream content, string query) {
        if (string.IsNullOrEmpty(query)) return content;
        using var reader = new StreamReader(content);
        var strContent = reader.ReadToEnd();
        var filtered = jmesPath.Transform(strContent, query);
        var bytes = Encoding.UTF8.GetBytes(filtered);
        return new MemoryStream(bytes);
    }

    public string FilterOutput(string content, string query) {
        if (string.IsNullOrEmpty(query)) return content;
        return jmesPath.Transform(content, query);
    }
}
