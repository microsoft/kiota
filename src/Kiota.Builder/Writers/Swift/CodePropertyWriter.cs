using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, SwiftConventionService>
{
    public CodePropertyWriter(SwiftConventionService conventionService) : base(conventionService) { }
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is not CodeElement parentElement) throw new InvalidOperationException("The parent of a property should be a class");
        var returnType = conventions.GetTypeString(codeElement.Type, parentElement);
        var accessModifier = conventions.GetAccessModifier(codeElement.Access);
        var defaultValue = codeElement.DefaultValue != null ? $" = {codeElement.DefaultValue}" : string.Empty;
        switch (codeElement.Kind)
        {
            default:
                writer.WriteLine($"{accessModifier} var {codeElement.Name.ToFirstCharacterLowerCase()}: {returnType}{defaultValue}");
                break;
        }
    }
}
