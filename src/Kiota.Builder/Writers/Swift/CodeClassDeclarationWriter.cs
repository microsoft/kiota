using System;
using System.Linq;
using System.Collections.Generic;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Swift;
public class CodeClassDeclarationWriter : CodeProprietableBlockDeclarationWriter<ClassDeclaration>
{
    public CodeClassDeclarationWriter(SwiftConventionService conventionService): base(conventionService) { }
    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer)
    {
        var derivedTypes = new List<string>{codeElement.Inherits?.Name}
                                        .Union(codeElement.Implements.Select(x => x.Name))
                                        .Where(x => x != null);
        var derivation = derivedTypes.Any() ? ": " +  derivedTypes.Select(x => x.ToFirstCharacterUpperCase()).Aggregate((x, y) => $"{x}, {y}") + " " : string.Empty;
        if(codeElement.Parent is CodeClass parentClass)
            conventions.WriteShortDescription(parentClass.Description, writer);
        writer.WriteLine($"public class {codeElement.Name.ToFirstCharacterUpperCase()} {derivation}{{");
        writer.IncreaseIndent();
    }
}
