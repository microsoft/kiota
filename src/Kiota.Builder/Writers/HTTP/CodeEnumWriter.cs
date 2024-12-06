using System;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Http;
public class CodeEnumWriter(HttpConventionService conventionService) : BaseElementWriter<CodeEnum, HttpConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
    }
}
