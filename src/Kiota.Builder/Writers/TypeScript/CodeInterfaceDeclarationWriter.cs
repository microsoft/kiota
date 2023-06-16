
using System;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript
{
    public class CodeInterfaceDeclarationWriter : BaseElementWriter<InterfaceDeclaration, TypeScriptConventionService>
    {
        private readonly CodeUsingWriter _codeUsingWriter;
        public CodeInterfaceDeclarationWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService)
        {
            _codeUsingWriter = new(clientNamespaceName);
        }

        public override void WriteCodeElement(InterfaceDeclaration codeElement, LanguageWriter writer)
        {
            ArgumentNullException.ThrowIfNull(codeElement);
            ArgumentNullException.ThrowIfNull(writer);

            var parentNamespace = codeElement.GetImmediateParentOfType<CodeNamespace>();
            _codeUsingWriter.WriteCodeElement(codeElement.Usings, parentNamespace, writer);

            var derivation = codeElement.Implements.Any() ? $" extends {codeElement.Implements.Select(static x => x.Name).Aggregate(static (x, y) => x + ", " + y)}" : string.Empty;
            writer.StartBlock($"export interface {codeElement.Name.ToFirstCharacterUpperCase()}{derivation} {{");
        }
    }
}
