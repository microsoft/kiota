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
        foreach (var currentElementEntry in GetEntry(currentElement).Where(static x => !string.IsNullOrEmpty(x)))
            yield return currentElementEntry;
        foreach (var childElement in currentElement.GetChildElements())
            foreach (var childElementEntry in GetEntriesFromDom(childElement))
                yield return childElementEntry;
    }
    private static IEnumerable<string> GetEntry(CodeElement codeElement, bool includeDefinitions = false)
    {
        if (codeElement is IAccessibleElement accessibleElement && accessibleElement.Access is AccessModifier.Private)
            return [];
        //TODO access modifiers
        //TODO static modifiers
        //TODO optional parameters
        return codeElement switch
        {
            CodeProperty property when property.Parent is not null =>
                [$"{GetEntryPath(property.Parent)}::{property.Name}:{GetEntryType(property.Type)}"],
            CodeMethod method when method.Parent is not null =>
                [$"{GetEntryPath(method.Parent)}::{method.Name}({GetParameters(method.Parameters)}):{GetEntryType(method.ReturnType)}"],
            CodeFunction function when function.Parent is not null =>
                [$"{GetEntryPath(function.Parent)}::{function.Name}({GetParameters(function.OriginalLocalMethod.Parameters)}):{GetEntryType(function.OriginalLocalMethod.ReturnType)}"],
            CodeIndexer codeIndexer when codeIndexer.Parent is not null =>
                [$"{GetEntryPath(codeIndexer.Parent)}::[{GetParameters([codeIndexer.IndexParameter])}]:{GetEntryType(codeIndexer.ReturnType)}"],
            CodeEnum codeEnum1 when !includeDefinitions =>
                codeEnum1.Options.Select((x, y) => $"{GetEntryPath(codeEnum1)}::{y:D4}-{x.Name}"),
            CodeClass codeClass1 when !includeDefinitions && codeClass1.StartBlock.Inherits is not null =>
                [$"{GetEntryPath(codeClass1)}{InheritsSymbol}{GetEntryType(codeClass1.StartBlock.Inherits)}"],
            CodeClass codeClass2 when !includeDefinitions && codeClass2.StartBlock.Implements.Any() =>
                [$"{GetEntryPath(codeClass2)}{ImplementsSymbol}{string.Join(", ", codeClass2.StartBlock.Implements.Select(static x => GetEntryType(x)))}"],
            CodeInterface codeInterface1 when !includeDefinitions && codeInterface1.StartBlock.Implements.Any() =>
                [$"{GetEntryPath(codeInterface1)}{ImplementsSymbol}{string.Join(", ", codeInterface1.StartBlock.Implements.Select(static x => GetEntryType(x)))}"],
            CodeClass codeClass when includeDefinitions => [GetEntryPath(codeClass)],
            CodeEnum codeEnum when includeDefinitions => [GetEntryPath(codeEnum)],
            CodeInterface codeInterface when includeDefinitions => [GetEntryPath(codeInterface)],
            _ => [],
        };
    }
    private const string InheritsSymbol = "-->";
    private const string ImplementsSymbol = "~~>";
    private static string GetParameters(IEnumerable<CodeParameter> parameters)
    {
        return string.Join(", ", parameters.Select(static x => $"{x.Name}:{GetEntryType(x.Type)}"));
    }
    private static string GetEntryType(CodeTypeBase codeElementTypeBase)
    {
        var collectionPrefix = codeElementTypeBase.IsArray ? "[" : codeElementTypeBase.CollectionKind is CodeTypeBase.CodeTypeCollectionKind.Complex ? "[" : string.Empty;
        var collectionSuffix = codeElementTypeBase.IsArray ? "]" : codeElementTypeBase.CollectionKind is CodeTypeBase.CodeTypeCollectionKind.Complex ? "]" : string.Empty;
        //TODO use the collection types from the convention service
        return codeElementTypeBase switch
        {
            CodeType codeElementType when codeElementType.TypeDefinition is not null => $"{collectionPrefix}{GetEntry(codeElementType.TypeDefinition, true).First()}{collectionSuffix}",
            CodeType codeElementType when codeElementType.TypeDefinition is null => $"{collectionPrefix}{codeElementType.Name}{collectionSuffix}",
            _ => $"{collectionPrefix}{codeElementTypeBase.Name}{collectionSuffix}",
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
