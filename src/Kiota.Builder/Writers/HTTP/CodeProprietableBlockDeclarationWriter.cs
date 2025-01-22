using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.Http;

public abstract class CodeProprietableBlockDeclarationWriter<T>(HttpConventionService conventionService) : BaseElementWriter<T, HttpConventionService>(conventionService)
    where T : ProprietableBlockDeclaration
{
    public override void WriteCodeElement(T codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        WriteTypeDeclaration(codeElement, writer);
    }
    protected abstract void WriteTypeDeclaration(T codeElement, LanguageWriter writer);
}
