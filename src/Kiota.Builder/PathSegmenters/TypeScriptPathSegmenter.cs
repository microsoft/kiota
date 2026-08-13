using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Configuration;
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
        modelsNamespace ??= currentElement.GetImmediateParentOfType<CodeNamespace>()?.GetRootNamespace().FindChildByName<CodeNamespace>($"{ClientNamespaceName}.{GenerationConfiguration.ModelsNamespaceSegmentName}");
        return currentElement switch
        {
            CodeFile currentFile when modelsNamespace is not null &&
                        currentElement.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace currentNamespace &&
                        !(modelsNamespace.IsParentOf(currentNamespace) || modelsNamespace == currentNamespace) &&
                        !currentFile.Interfaces.Any(static x => x.Kind is CodeInterfaceKind.RequestBuilder && x.OriginalClass is not null && x.OriginalClass.Methods.Any(static y => y.Kind is CodeMethodKind.ClientConstructor))
                    => IndexFileName,
            _ => GetDefaultFileName(currentElement),
        };
    }
    private static string GetDefaultFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterLowerCase();
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
}
