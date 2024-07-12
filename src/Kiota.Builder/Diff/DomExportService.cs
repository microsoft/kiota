using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Diff;

internal class DomExportService
{
    internal DomExportService(string outputDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectoryPath);
        OutputDirectoryPath = outputDirectoryPath;
    }
    private readonly string OutputDirectoryPath;
    private const string DomExportFileName = "kiota-dom-export.txt";
    internal async Task SerializeDomAsync(CodeNamespace rootNamespace, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(OutputDirectoryPath, DomExportFileName);
        using var fileStream = File.Create(filePath);
        var entries = GetEntriesFromDom(rootNamespace).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        var content = string.Join(Environment.NewLine, entries);
        var contentBytes = System.Text.Encoding.UTF8.GetBytes(content);
        await fileStream.WriteAsync(contentBytes, cancellationToken).ConfigureAwait(false);
    }
    private static IEnumerable<string> GetEntriesFromDom(CodeElement currentElement)
    {
        if (GetEntry(currentElement) is string currentElementEntry && !string.IsNullOrEmpty(currentElementEntry))
            yield return currentElementEntry;
        foreach (var childElement in currentElement.GetChildElements())
            foreach (var childElementEntry in GetEntriesFromDom(childElement))
                yield return childElementEntry;
    }
    private static string GetEntry(CodeElement codeElement)
    {
        if (codeElement is IAccessibleElement accessibleElement && accessibleElement.Access is AccessModifier.Private)
            return string.Empty;
        return codeElement switch
        {
            CodeProperty property when property.Parent is not null =>
                $"{GetEntryPath(property.Parent)}::{property.Name}:{GetEntryType(property.Type)}",
            //TODO method
            //TODO index
            //TODO enum member
            //TODO functions
            //TODO class/interface inheritance
            _ => string.Empty,
        };
    }
    private static string GetEntryType(CodeTypeBase codeElementTypeBase)
    {
        return codeElementTypeBase switch
        {
            CodeType codeElementType when codeElementType.TypeDefinition is not null => GetEntry(codeElementType.TypeDefinition),
            CodeType codeElementType when codeElementType.TypeDefinition is null => codeElementType.Name,
            _ => codeElementTypeBase.Name,
        };
    }
    private static string GetEntryPath(CodeElement codeElement)
    {
        return codeElement switch
        {
            CodeClass x when x.Parent is not null => $"{GetEntryPath(x.Parent)}.{codeElement.Name}",
            CodeEnum x when x.Parent is not null => $"{GetEntryPath(x.Parent)}.{codeElement.Name}",
            CodeInterface x when x.Parent is not null => $"{GetEntryPath(x.Parent)}.{codeElement.Name}",
            CodeNamespace => $"{codeElement.Name}",
            _ => string.Empty,
        };
    }
}
