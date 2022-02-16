using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript {
    public class CodeClassDeclarationWriter : BaseElementWriter<CodeClass.Declaration, TypeScriptConventionService>
    {
        private readonly CodeUsingWriter _codeUsingWriter;
        public CodeClassDeclarationWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService){
            _codeUsingWriter = new (clientNamespaceName);
        }
        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
            _codeUsingWriter.WriteCodeElement(codeElement.Usings, parentNamespace, writer);
            
            var inheritSymbol = conventions.GetTypeString(codeElement.Inherits, codeElement);
            var derivation = (inheritSymbol == null ? string.Empty : $" extends {inheritSymbol}") +
                            (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            conventions.WriteShortDescription((codeElement.Parent as CodeClass).Description, writer);
            writer.WriteLine($"export class {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
            writer.IncreaseIndent();
        }
        
    }
}
