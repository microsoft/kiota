using Kiota.Builder.Writers.CSharp;
using Kiota.Builder.Writers.TypeScript;

namespace Kiota.Builder.Writers {
    public class CodeTypeWriter : BaseElementWriter<CodeType, ILanguageConventionService>
    {
        public CodeTypeWriter(ILanguageConventionService conventionService) : base(conventionService){}
        public override void WriteCodeElement(CodeType codeElement, LanguageWriter writer)
        {
            if(conventions is TypeScriptConventionService tsConventions)
                writer.Write(tsConventions.GetTypeString(codeElement, codeElement), includeIndent: false);
            else if (conventions is CSharpConventionService csConventions)
                writer.Write(csConventions.GetTypeString(codeElement, codeElement), includeIndent: false);
            else
                writer.Write(conventions.GetTypeString(codeElement), includeIndent: false);
        }
    }
}
