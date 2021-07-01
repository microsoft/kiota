using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodeEnumWriter : BaseElementWriter<CodeEnum, RubyConventionService>
    {
        public CodeEnumWriter(RubyConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeEnum codeElement, LanguageWriter writer)
        {
            if(!codeElement.Options.Any())
                return;
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.WriteLine($"module {codeElement.Parent.Parent.Name.Split('.').Select(x => x.ToFirstCharacterUpperCase()).Aggregate((z,y) => z + "::" + y)}");
                writer.IncreaseIndent();
            }
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"{codeElement.Name.ToFirstCharacterUpperCase()} = {{");
            writer.IncreaseIndent();
            codeElement.Options.ForEach(x => writer.WriteLine($"{x.ToFirstCharacterUpperCase()}: :{x.ToFirstCharacterUpperCase()},"));
            writer.DecreaseIndent();
            writer.WriteLine("}");
            if(codeElement?.Parent?.Parent is CodeNamespace) {
                writer.DecreaseIndent();
                writer.WriteLine("end");
            }
            
        }
    }
}
