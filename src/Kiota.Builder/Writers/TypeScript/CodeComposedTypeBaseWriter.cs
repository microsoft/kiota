using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public abstract class CodeComposedTypeBaseWriter<TCodeComposedTypeBase, TConventionsService>(TypeScriptConventionService conventionService) : BaseElementWriter<TCodeComposedTypeBase, TypeScriptConventionService>(conventionService) where TCodeComposedTypeBase : CodeComposedTypeBase
{
    public abstract string TypesDelimiter
    {
        get;
    }

    public override void WriteCodeElement(TCodeComposedTypeBase codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (!codeElement.Types.Any())
            throw new InvalidOperationException("CodeComposedTypeBase should be comprised of one or more types.");

        var codeUnionString = string.Join($" {TypesDelimiter} ", codeElement.Types.Select(x => conventions.GetTypeString(x, codeElement)));

        writer.WriteLine($"export type {codeElement.Name.ToFirstCharacterUpperCase()} = {codeUnionString};");
    }
}
