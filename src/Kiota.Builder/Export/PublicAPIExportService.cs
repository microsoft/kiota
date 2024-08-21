using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
using Kiota.Builder.Writers;
using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.Go;
using Kiota.Builder.Writers.Java;
using Kiota.Builder.Writers.Php;
using Kiota.Builder.Writers.Python;
using Kiota.Builder.Writers.Ruby;
using Kiota.Builder.Writers.Swift;
using Kiota.Builder.Writers.TypeScript;

namespace Kiota.Builder.Export;

internal class PublicApiExportService
{
    internal PublicApiExportService(GenerationConfiguration generationConfiguration)
    {
        ArgumentNullException.ThrowIfNull(generationConfiguration);
        _outputDirectoryPath = generationConfiguration.OutputPath;
        _conventionService = GetLanguageConventionServiceFromConfiguration(generationConfiguration);
    }
    private readonly string _outputDirectoryPath;
    private readonly ILanguageConventionService _conventionService;
    private const string DomExportFileName = "kiota-dom-export.txt";
    private const string InheritsSymbol = "-->";
    private const string ImplementsSymbol = "~~>";
    private const string OptionalSymbol = "?";
    private const string ParentElementAndChildSeparator = "::";

    internal async Task SerializeDomAsync(CodeNamespace rootNamespace, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_outputDirectoryPath, DomExportFileName);
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
    private IEnumerable<string> GetEntriesFromDom(CodeElement currentElement)
    {
        foreach (var currentElementEntry in GetEntry(currentElement).Where(static x => !string.IsNullOrEmpty(x)))
            yield return currentElementEntry;
        foreach (var childElement in currentElement.GetChildElements())
            foreach (var childElementEntry in GetEntriesFromDom(childElement))
                yield return childElementEntry;
    }

    private IEnumerable<string> GetEntry(CodeElement codeElement, bool includeDefinitions = false)
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
                [$"{GetEntryPath(property.Parent)}{ParentElementAndChildSeparator}{accessModifierValue}{property.Name}:{GetEntryType(property.Type, property)}"],
            CodeMethod method when method.Parent is not null =>
                [$"{GetEntryPath(method.Parent)}{ParentElementAndChildSeparator}{(method.IsStatic ? "|static" : string.Empty)}{accessModifierValue}{method.Name}({GetParameters(method.Parameters)}):{((GetEntryType(method.ReturnType, method) is { } stringValue && !string.IsNullOrEmpty(stringValue)) ? stringValue : method.ReturnType.Name)}"],
            CodeFunction function when function.Parent is not null =>
                [$"{GetEntryPath(function.Parent)}{ParentElementAndChildSeparator}{function.Name}({GetParameters(function.OriginalLocalMethod.Parameters)}):{GetEntryType(function.OriginalLocalMethod.ReturnType, function)}"],
            CodeIndexer codeIndexer when codeIndexer.Parent is not null =>
                [$"{GetEntryPath(codeIndexer.Parent)}{ParentElementAndChildSeparator}[{GetParameters([codeIndexer.IndexParameter])}]:{GetEntryType(codeIndexer.ReturnType, codeIndexer)}"],
            CodeEnum codeEnum1 when !includeDefinitions =>
                codeEnum1.Options.Select((x, y) => $"{GetEntryPath(codeEnum1)}::{y:D4}-{x.Name}"),
            CodeClass codeClass1 when !includeDefinitions && codeClass1.StartBlock.Inherits is not null =>
                [$"{GetEntryPath(codeClass1)}{InheritsSymbol}{GetEntryType(codeClass1.StartBlock.Inherits, codeClass1)}"],
            CodeClass codeClass2 when !includeDefinitions && codeClass2.StartBlock.Implements.Any() =>
                [$"{GetEntryPath(codeClass2)}{ImplementsSymbol}{string.Join("; ", codeClass2.StartBlock.Implements.Select(x => GetEntryType(x, codeClass2)))}"],
            CodeInterface codeInterface1 when !includeDefinitions && codeInterface1.StartBlock.Implements.Any() =>
                [$"{GetEntryPath(codeInterface1)}{ImplementsSymbol}{string.Join("; ", codeInterface1.StartBlock.Implements.Select(x => GetEntryType(x, codeInterface1)))}"],
            CodeClass codeClass when includeDefinitions => [GetEntryPath(codeClass)],
            CodeEnum codeEnum when includeDefinitions => [GetEntryPath(codeEnum)],
            CodeInterface codeInterface when includeDefinitions => [GetEntryPath(codeInterface)],
            CodeConstant codeConstant => [GetEntryPath(codeConstant)],
            _ => [],
        };
    }
    private string GetParameters(IEnumerable<CodeParameter> parameters)
    {
        return string.Join("; ", parameters.Select(x => $"{x.Name}{(x.Optional ? OptionalSymbol : string.Empty)}:{GetEntryType(x.Type, x)}{(string.IsNullOrEmpty(x.DefaultValue) ? string.Empty : $"={x.DefaultValue}")}"));
    }

    private string GetEntryType(CodeTypeBase codeElementTypeBase, CodeElement targetElement) => _conventionService.GetTypeString(codeElementTypeBase, targetElement)
        .Replace(ParentElementAndChildSeparator, ".", StringComparison.OrdinalIgnoreCase);//ensure language specific stuff doesn't break things like global:: in dotnet

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
    private static ILanguageConventionService GetLanguageConventionServiceFromConfiguration(GenerationConfiguration generationConfiguration)
    {
        return generationConfiguration.Language switch
        {
            GenerationLanguage.CSharp => new CSharpConventionService(),
            GenerationLanguage.Java => new JavaConventionService(),
            GenerationLanguage.TypeScript => new TypeScriptConventionService(),
            GenerationLanguage.PHP => new PhpConventionService(),
            GenerationLanguage.Python => new PythonConventionService(),
            GenerationLanguage.Go => new GoConventionService(),
            GenerationLanguage.Swift => new SwiftConventionService(generationConfiguration.ClientNamespaceName),
            GenerationLanguage.Ruby => new RubyConventionService(),
            GenerationLanguage.CLI => new CSharpConventionService(),
            _ => throw new ArgumentOutOfRangeException(nameof(generationConfiguration), generationConfiguration.Language, null)
        };
    }
}
