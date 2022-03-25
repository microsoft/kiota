using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift;
public class CodePropertyWriter : BaseElementWriter<CodeProperty, SwiftConventionService>
{
    public CodePropertyWriter(SwiftConventionService conventionService) : base(conventionService) {}
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        var returnType = conventions.GetTypeString(codeElement.Type, codeElement.Parent);
        var accessModifier = conventions.GetAccessModifier(codeElement.Access);
        switch(codeElement.Kind) {
            default:
                writer.WriteLine($"{accessModifier} var {codeElement.Name.ToFirstCharacterLowerCase()}: {returnType}");
            break;
        }
    }
}
