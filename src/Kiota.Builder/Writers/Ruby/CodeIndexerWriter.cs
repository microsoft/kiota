using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Ruby {
    public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, RubyConventionService>
    {
        public CodeIndexerWriter(RubyConventionService conventionService) : base(conventionService) {}
        public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
        {
            var returnType = conventions.GetTypeString(codeElement.ReturnType);
            var currentPathProperty = codeElement.Parent.GetChildElements(true).OfType<CodeProperty>().FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"def [](position)");
            writer.IncreaseIndent();
            conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, " + \"/\" + position", "return ");
            writer.DecreaseIndent();
            writer.WriteLine("end");
        }
    }
}
