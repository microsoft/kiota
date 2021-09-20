using System.Linq;

namespace Kiota.Builder.Writers.CSharp {
    public class CodeIndexerWriter : BaseElementWriter<CodeIndexer, CSharpConventionService>
    {
        public CodeIndexerWriter(CSharpConventionService conventionService) : base(conventionService) {}
        public override void WriteCodeElement(CodeIndexer codeElement, LanguageWriter writer)
        {
            var currentPathProperty = (codeElement.Parent as CodeClass).Properties.FirstOrDefault(x => x.IsOfKind(CodePropertyKind.CurrentPath));
            var returnType = conventions.GetTypeString(codeElement.ReturnType);
            conventions.WriteShortDescription(codeElement.Description, writer);
            writer.WriteLine($"public {returnType} this[{conventions.GetTypeString(codeElement.IndexType)} position] {{ get {{");
            writer.IncreaseIndent();
            conventions.AddRequestBuilderBody(currentPathProperty != null, returnType, writer, " + \"/\" + position", "return ");
            writer.DecreaseIndent();
            writer.WriteLine("} }");
        }
    }
}
