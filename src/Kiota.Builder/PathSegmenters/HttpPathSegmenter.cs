using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.PathSegmenters;
public class HttpPathSegmenter(string rootPath, string clientNamespaceName) : CommonPathSegmenter(rootPath, clientNamespaceName)
{
    public override string FileSuffix => ".http";
    public override string NormalizeNamespaceSegment(string segmentName) => segmentName.ToFirstCharacterUpperCase();
    public override string NormalizeFileName(CodeElement currentElement)
    {
        var fileName = GetLastFileNameSegment(currentElement).ToFirstCharacterUpperCase();
        var suffix = currentElement switch
        {
            CodeNamespace n => n.Name.GetNamespaceImportSymbol(string.Empty),
            CodeClass c => c.GetImmediateParentOfType<CodeNamespace>().Name.GetNamespaceImportSymbol(string.Empty),
            _ => string.Empty,
        };
        return fileName + suffix;
    }
}
