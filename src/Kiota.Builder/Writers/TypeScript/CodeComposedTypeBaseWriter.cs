using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript;

public abstract class CodeComposedTypeBaseWriter<TCodeComposedTypeBase, TConventionsService> : BaseElementWriter<TCodeComposedTypeBase, TConventionsService> where TCodeComposedTypeBase : CodeComposedTypeBase where TConventionsService : TypeScriptConventionService
{
    protected CodeComposedTypeBaseWriter(TConventionsService conventionService) : base(conventionService)
    {
    }
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

        // TODO: documentation info
        writer.WriteLine($"export type {codeElement.Name.ToFirstCharacterUpperCase()} = {codeUnionString};");
    }
}
