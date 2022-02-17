using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Go;

public class CodeClassDeclarationWriter : CodeProprietableBlockDeclarationWriter<ClassDeclaration> {
    public CodeClassDeclarationWriter(GoConventionService conventionService) : base(conventionService) {}
    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer) {
        var className = codeElement.Name.ToFirstCharacterUpperCase();
        var currentClass = codeElement.Parent as CodeClass;
        conventions.WriteShortDescription($"{className} {currentClass.Description.ToFirstCharacterLowerCase()}", writer);
        writer.WriteLine($"type {className} struct {{");
        writer.IncreaseIndent();
        if(codeElement.Inherits?.AllTypes?.Any() ?? false) {
            var parentTypeName = conventions.GetTypeString(codeElement.Inherits.AllTypes.First(), currentClass, true, false);
            writer.WriteLine($"{parentTypeName}");
        }
    }
}
