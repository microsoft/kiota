using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;
public class TypeScriptPathSegmenter : CommonPathSegmenter
{
    private CodeNamespace? modelsNamespace;
    public TypeScriptPathSegmenter(string rootPath, string clientNamespaceName) : base(rootPath, clientNamespaceName) { }
    public override string FileSuffix => ".ts";
    private const string IndexFileName = "index";
    public override string NormalizeFileName(CodeElement currentElement)
    {
        ArgumentNullException.ThrowIfNull(currentElement);
        modelsNamespace ??= currentElement.GetImmediateParentOfType<CodeNamespace>()?.GetRootNamespace().FindChildByName<CodeNamespace>($"{ClientNamespaceName}.models");
        return currentElement switch
        {
            CodeNamespace => IndexFileName,
            CodeFile when modelsNamespace is not null &&
                        currentElement.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace currentNamespace &&
                        !modelsNamespace.IsParentOf(currentNamespace)
                    => IndexFileName,
            _ => GetDefaultFileName(currentElement),
        };
    }
    private static string GetDefaultFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterLowerCase();
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
    public override IEnumerable<string> GetAdditionalSegment(CodeElement currentElement, string fileName)
    {
        return currentElement switch
        {
            CodeNamespace => new[] { GetDefaultFileName(currentElement) },// We put barrels inside namespace folders
            _ => Enumerable.Empty<string>(),
        };
    }
}
