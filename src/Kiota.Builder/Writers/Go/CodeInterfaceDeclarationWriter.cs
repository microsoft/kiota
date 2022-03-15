using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public class CodeInterfaceDeclarationWriter : CodeProprietableBlockDeclarationWriter<InterfaceDeclaration>
{
    public CodeInterfaceDeclarationWriter(GoConventionService conventionService) : base(conventionService){}
    protected override void WriteTypeDeclaration(InterfaceDeclaration codeElement, LanguageWriter writer)
    {
        var inter = codeElement.Parent as CodeInterface;
        var interName = codeElement.Name.ToFirstCharacterUpperCase();
        conventions.WriteShortDescription($"{interName} {inter.Description.ToFirstCharacterLowerCase()}", writer);
        writer.WriteLine($"type {interName} interface {{");
        writer.IncreaseIndent();
        foreach (var implement in codeElement.Implements) {
            var parentTypeName = conventions.GetTypeString(implement, inter, true, false);
            writer.WriteLine($"{parentTypeName}");
        }
    }
}
