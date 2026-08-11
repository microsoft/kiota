using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Ruby;

public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, RubyConventionService>
{
    private const string RequireDirective = "require_relative";
    private readonly RubyPathSegmenter PathSegmenter;
    public CodeNamespaceWriter(RubyConventionService conventionService, RubyPathSegmenter pathSegmenter) : base(conventionService)
    {
        ArgumentNullException.ThrowIfNull(pathSegmenter);
        PathSegmenter = pathSegmenter;
    }
    public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        foreach (var childModel in codeElement.GetChildElements(true).OfType<CodeEnum>().OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase))
            writer.WriteLine($"{RequireDirective} '{PathSegmenter.GetRelativeFileName(codeElement, childModel).ToSnakeCase()}'");
        NamespaceClassNamesProvider.WriteClassesInOrderOfInheritance(codeElement, x => writer.WriteLine($"{RequireDirective} '{PathSegmenter.GetRelativeFileName(codeElement, x).ToSnakeCase()}'"));
    }

}
