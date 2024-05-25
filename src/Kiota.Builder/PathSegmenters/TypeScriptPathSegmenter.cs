using System;

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

        switch (currentElement)
        {
            case CodeFile currentFile when modelsNamespace is not null &&
                        currentElement.GetImmediateParentOfType<CodeNamespace>() is CodeNamespace currentNamespace &&
                        !(modelsNamespace.IsParentOf(currentNamespace) || modelsNamespace == currentNamespace):
                foreach (var interfaceElement in currentFile.Interfaces)
                {
                    if (interfaceElement.Kind is CodeInterfaceKind.RequestBuilder && interfaceElement.OriginalClass is not null)
                    {
                        foreach (var method in interfaceElement.OriginalClass.Methods)
                        {
                            if (method.Kind is CodeMethodKind.ClientConstructor)
                            {
                                return GetDefaultFileName(currentElement);
                            }
                        }
                    }
                }
                return IndexFileName;
            default:
                return GetDefaultFileName(currentElement);
        }
    }
    private static string GetDefaultFileName(CodeElement currentElement) => GetLastFileNameSegment(currentElement).ToFirstCharacterLowerCase();
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterLowerCase();
}
