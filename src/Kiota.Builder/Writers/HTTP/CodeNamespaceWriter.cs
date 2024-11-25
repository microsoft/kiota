using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Http;
public class CodeNamespaceWriter(HttpConventionService conventionService) : BaseElementWriter<CodeNamespace, HttpConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeNamespace codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
    }
}
