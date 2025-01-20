using System;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers;

public class GenericElementWriter(ILanguageConventionService conventionService) : BaseElementWriter<CodeElement, ILanguageConventionService>(conventionService)
{
    public override void WriteCodeElement(CodeElement codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
    }
}
