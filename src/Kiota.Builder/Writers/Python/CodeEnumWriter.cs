using System.Linq;

using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python;
public class CodeEnumWriter : BaseElementWriter<CodeEnum, PythonConventionService>
{
    public CodeEnumWriter(PythonConventionService conventionService) : base(conventionService){}
    public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
    {
        writer.WriteLine("from enum import Enum");
        writer.WriteLine();
        writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}(Enum):");
        writer.IncreaseIndent();
        if(!codeElement.Options.Any()){
            writer.WriteLine("pass");
        }
        else {
            codeElement.Options.ToList().ForEach(x => {
            conventions.WriteInLineDescription(x.Documentation.Description, writer);
            writer.WriteLine($"{x.Name.ToFirstCharacterUpperCase()} = \"{x.SerializationName ?? x.Name}\",");
            });
        }
    }
}
