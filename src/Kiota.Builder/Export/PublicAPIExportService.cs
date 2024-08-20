using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Export;

internal class PublicApiExportService
{
    internal PublicApiExportService(string outputDirectoryPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(outputDirectoryPath);
        OutputDirectoryPath = outputDirectoryPath;
    }
    private readonly string OutputDirectoryPath;
    private const string DomExportFileName = "kiota-dom-export.txt";
    private const string InheritsSymbol = "-->";
    private const string ImplementsSymbol = "~~>";
    private const string OptionalSymbol = "?";
    internal async Task SerializeDomAsync(CodeNamespace rootNamespace, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(OutputDirectoryPath, DomExportFileName);
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
        await using var fileStream = File.Create(filePath);
        await using var streamWriter = new StreamWriter(fileStream);
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
        var entries = GetEntriesFromDom(rootNamespace).Order(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var entry in entries)
        {
            await streamWriter.WriteLineAsync(entry.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
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
        string accessModifierValue = string.Empty;
        if (codeElement is IAccessibleElement accessibleElement)
        {
            if (accessibleElement.Access is AccessModifier.Private)
                return []; // we are not interested in private props as they are not used externally

            accessModifierValue = $"|{accessibleElement.Access.ToString().ToLowerInvariant()}|";
        }

        return codeElement switch
        {
            CodeProperty property when property.Parent is not null =>
                [$"{GetEntryPath(property.Parent)}::{accessModifierValue}{property.Name}:{GetEntryType(property.Type)}"],
            CodeMethod method when method.Parent is not null =>
                [$"{GetEntryPath(method.Parent)}::{(method.IsStatic ? "|static" : string.Empty)}{accessModifierValue}{method.Name}({GetParameters(method.Parameters)}):{GetEntryType(method.ReturnType)}"],
            CodeFunction function when function.Parent is not null =>
                [$"{GetEntryPath(function.Parent)}::{function.Name}({GetParameters(function.OriginalLocalMethod.Parameters)}):{GetEntryType(function.OriginalLocalMethod.ReturnType)}"],
            CodeIndexer codeIndexer when codeIndexer.Parent is not null =>
                [$"{GetEntryPath(codeIndexer.Parent)}::[{GetParameters([codeIndexer.IndexParameter])}]:{GetEntryType(codeIndexer.ReturnType)}"],
            CodeEnum codeEnum1 when !includeDefinitions =>
                codeEnum1.Options.Select((x, y) => $"{GetEntryPath(codeEnum1)}::{y:D4}-{x.Name}"),
            CodeClass codeClass1 when !includeDefinitions && codeClass1.StartBlock.Inherits is not null =>
                [$"{GetEntryPath(codeClass1)}{InheritsSymbol}{GetEntryType(codeClass1.StartBlock.Inherits)}"],
            CodeClass codeClass2 when !includeDefinitions && codeClass2.StartBlock.Implements.Any() =>
                [$"{GetEntryPath(codeClass2)}{ImplementsSymbol}{string.Join("; ", codeClass2.StartBlock.Implements.Select(static x => GetEntryType(x)))}"],
            CodeInterface codeInterface1 when !includeDefinitions && codeInterface1.StartBlock.Implements.Any() =>
                [$"{GetEntryPath(codeInterface1)}{ImplementsSymbol}{string.Join("; ", codeInterface1.StartBlock.Implements.Select(static x => GetEntryType(x)))}"],
            CodeClass codeClass when includeDefinitions => [GetEntryPath(codeClass)],
            CodeEnum codeEnum when includeDefinitions => [GetEntryPath(codeEnum)],
            CodeInterface codeInterface when includeDefinitions => [GetEntryPath(codeInterface)],
            CodeConstant codeConstant => [GetEntryPath(codeConstant)],
            _ => [],
        };
    }
    private static string GetParameters(IEnumerable<CodeParameter> parameters)
    {
        return string.Join("; ", parameters.Select(static x => $"{x.Name}{(x.Optional ? OptionalSymbol : string.Empty)}:{GetEntryType(x.Type)}{(string.IsNullOrEmpty(x.DefaultValue) ? string.Empty : $"={x.DefaultValue}")}"));
    }
    private static string GetEntryType(CodeTypeBase codeElementTypeBase)
    {
        var collectionPrefix = codeElementTypeBase.IsArray || codeElementTypeBase.CollectionKind is CodeTypeBase.CodeTypeCollectionKind.Complex ? "[" : string.Empty;
        var collectionSuffix = codeElementTypeBase.IsArray || codeElementTypeBase.CollectionKind is CodeTypeBase.CodeTypeCollectionKind.Complex ? "]" : string.Empty;
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
            CodeConstant x when x.Parent is not null => $"{GetEntryPath(x.Parent)}.{codeElement.Name}",
            CodeFile x when x.Parent is not null => GetEntryPath(x.Parent),
            CodeNamespace x => x.Name,
            _ => string.Empty,
        };
    }
}
