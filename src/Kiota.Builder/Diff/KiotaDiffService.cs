using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Diff;

public class KiotaDiffService
{
    public KiotaDiffService(string outputDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectoryPath);
        OutputDirectoryPath = outputDirectoryPath;
    }
    private static readonly JsonSerializerOptions options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
        }
    };
    private static readonly DomJsonSerializationContext context = new(options);
    private readonly string OutputDirectoryPath;
    private const string DomExportFileName = "diff.json";
    public async Task SerializeDomAsync(CodeNamespace rootNamespace, CancellationToken cancellationToken = default)
    {
        //TODO anything that's an array needs to be replaced by a dictionary for the semantic comparison to work
        var filePath = Path.Combine(OutputDirectoryPath, DomExportFileName);
        using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, rootNamespace, context.CodeNamespace, cancellationToken).ConfigureAwait(false);
    }
    public async Task SerializeDomAsync(CodeNamespace rootNamespace, Stream outputStream, CancellationToken cancellationToken = default)
    {
        await JsonSerializer.SerializeAsync(outputStream, rootNamespace, context.CodeNamespace, cancellationToken).ConfigureAwait(false);
    }
    public async Task<CodeNamespace?> DeserializeDomAsync(Stream inputStream, CancellationToken cancellationToken = default)
    {
        return await JsonSerializer.DeserializeAsync(inputStream, context.CodeNamespace, cancellationToken).ConfigureAwait(false);
    }
}
