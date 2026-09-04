using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.Writers.Go;

namespace Kiota.Builder.PathSegmenters;

public class DartPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    public override string FileSuffix => ".dart";

    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToCamelCase();

    private readonly ConcurrentDictionary<CodeNamespace, Dictionary<string, CodeElement[]>> collidingFileNames = new();
    /// <summary>
    /// Snake casing is lossy: names that differ only in where a separator falls, such as
    /// Process_error (an inline property type) and ProcessError (a component schema), collapse onto
    /// the same file name. One model then silently overwrote the other, so the surviving file did not
    /// declare the type the imports asked for and the client did not compile. The second and later
    /// members of a colliding set get a numeric suffix; the ordering is by ordinal name, which keeps
    /// the result stable between the import path and the output path.
    /// </summary>
    public override string NormalizeFileName(CodeElement currentElement)
    {
        var fileName = GetLastFileNameSegment(currentElement).ToSnakeCase();
        if (currentElement is not (CodeClass or CodeEnum) || currentElement.Parent is not CodeNamespace parentNamespace)
            return fileName;
        var collisions = collidingFileNames.GetOrAdd(parentNamespace, static ns =>
            ns.Classes.Cast<CodeElement>()
                .Concat(ns.Enums)
                .GroupBy(static x => GetLastFileNameSegment(x).ToSnakeCase(), StringComparer.OrdinalIgnoreCase)
                .Where(static x => x.Skip(1).Any())
                .ToDictionary(static x => x.Key,
                              static x => x.OrderBy(static y => y.Name, StringComparer.Ordinal).ToArray(),
                              StringComparer.OrdinalIgnoreCase));
        if (!collisions.TryGetValue(fileName, out var siblings))
            return fileName;
        var index = Array.FindIndex(siblings, x => ReferenceEquals(x, currentElement));
        return index > 0 ? $"{fileName}_{index + 1}" : fileName;
    }

    internal string GetRelativeFileName(CodeNamespace @namespace, CodeElement element)
    {
        // the import manager passes the using's CodeType; resolve it to the definition the file is named after
        return element is CodeType { TypeDefinition: CodeElement typeDefinition } ?
            NormalizeFileName(typeDefinition) :
            NormalizeFileName(element);
    }
}
