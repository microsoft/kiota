using System;
using System.Linq;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.http;
public class CodeMethodWriter(HttpConventionService conventionService) : BaseElementWriter<CodeMethod, HttpConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeMethod codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
    }
}
