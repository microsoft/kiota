using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Python {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, PythonConventionService>
    {
        public CodeEnumWriter(PythonConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            if(!codeElement.Options.Any())
                return;
            writer.WriteLine($"from enum import Enum");
            writer.WriteLine();
            writer.WriteLine($"class {codeElement.Name.ToFirstCharacterUpperCase()}(Enum):");
            writer.IncreaseIndent();
            codeElement.Options.ToList().ForEach(x => writer.WriteLine($"{x} = '{x}'"));
        }
    }
}
