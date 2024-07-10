using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Diff;

public class DomExportService
{
    public DomExportService(string outputDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectoryPath);
        OutputDirectoryPath = outputDirectoryPath;
    }
    private readonly string OutputDirectoryPath;
    private const string DomExportFileName = "kiota-dom-export.txt";
    public async Task SerializeDomAsync(CodeNamespace rootNamespace, CancellationToken cancellationToken = default)
    {
        //TODO there are things were order matter like enum options and parameters
        var filePath = Path.Combine(OutputDirectoryPath, DomExportFileName);
        using var fileStream = File.Create(filePath);
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        //TODO implement linear export
    }
}
