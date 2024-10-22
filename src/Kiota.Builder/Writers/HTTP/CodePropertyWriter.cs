using System;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.http;
public class CodePropertyWriter(HttpConventionService conventionService) : BaseElementWriter<CodeProperty, HttpConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeProperty codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        if (codeElement.Parent is null) throw new InvalidOperationException("The parent of a property should be a class");
    }
}
