using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Kiota.Builder.PathSegmenters;

namespace Kiota.Builder.Writers.Ruby;

public class CodeNamespaceWriter : BaseElementWriter<CodeNamespace, RubyConventionService>
{
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
        // registering instead of requiring lets a class load its base on demand, in any order
        var autoloaded = new List<CodeElement>(codeElement.GetChildElements(true).OfType<CodeEnum>()
                                                        .Where(RubyConventionService.IsAutoloaded)
                                                        .OrderBy(static x => x.Name, StringComparer.OrdinalIgnoreCase));
        NamespaceClassNamesProvider.WriteClassesInOrderOfInheritance(codeElement, x =>
        {
            if (RubyConventionService.IsAutoloaded(x)) autoloaded.Add(x);
        });
        if (autoloaded.Count == 0) return;
        conventions.WriteNamespaceModules(codeElement, writer);
        foreach (var element in autoloaded)
            writer.WriteLine($"autoload :{element.Name.ToFirstCharacterUpperCase()}, ::File.expand_path('{PathSegmenter.GetRelativeFileName(codeElement, element).ToSnakeCase()}', __dir__)");
        conventions.WriteNamespaceClosing(codeElement, writer);
    }
}
