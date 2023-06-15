using System;
using System.Collections.Generic;
using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift;
public class CodeClassDeclarationWriter : CodeProprietableBlockDeclarationWriter<ClassDeclaration>
{
    public CodeClassDeclarationWriter(SwiftConventionService conventionService) : base(conventionService) { }
    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        var derivedTypes = new List<string?> { codeElement.Inherits?.Name }
                                        .Union(codeElement.Implements.Select(static x => x.Name))
                                        .Where(static x => x != null)
                                        .ToArray();
        var derivation = derivedTypes.Any() ? ": " + derivedTypes.Select(x => x.ToFirstCharacterUpperCase()).Aggregate(static (x, y) => $"{x}, {y}") + " " : string.Empty;
        if (codeElement.Parent is CodeClass parentClass)
            conventions.WriteShortDescription(parentClass.Documentation.Description, writer);
        writer.WriteLine($"public class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}{{");
        writer.IncreaseIndent();
    }
}
