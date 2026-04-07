using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;

public class RustPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    public override string FileSuffix => ".rs";
    public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
    {
        if (currentElement is CodeNamespace ns && IsRootNamespace(ns))
            return Enumerable.Empty<string>(); // lib.rs at output root, no subdirectory

        return currentElement switch
        {
            CodeNamespace => new[] { GetLastFileNameSegment(currentElement) },
            _ => Enumerable.Empty<string>(),
        };
    }

    public override string NormalizeFileName(CodeElement currentElement)
    {
        if (currentElement is CodeNamespace ns && IsRootNamespace(ns))
            return "lib"; // root namespace becomes lib.rs

        return currentElement switch
        {
            CodeNamespace => "mod",
            _ => GetLastFileNameSegment(currentElement).ToSnakeCase(),
        };
    }

    public override string NormalizeNamespaceSegment(string segmentName) => segmentName?.ToSnakeCase() ?? string.Empty;

    private bool IsRootNamespace(CodeNamespace ns)
    {
        return ns.Name.Equals(ClientNamespaceName, StringComparison.OrdinalIgnoreCase);
    }
}
