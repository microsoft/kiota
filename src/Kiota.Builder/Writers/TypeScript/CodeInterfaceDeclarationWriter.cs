
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

        public override void WriteCodeElement(InterfaceDeclaration interfaceDeclaration, LanguageWriter writer)
        {
            ArgumentNullException.ThrowIfNull(interfaceDeclaration);
            ArgumentNullException.ThrowIfNull(writer);

            var parentNamespace = interfaceDeclaration.GetImmediateParentOfType<CodeNamespace>();
            _codeUsingWriter.WriteCodeElement(interfaceDeclaration.Usings, parentNamespace, writer);

            var derivation = interfaceDeclaration.Implements.Any() ? $" extends {interfaceDeclaration.Implements.Select(x => x.Name).Aggregate((x, y) => x + ", " + y)}" : string.Empty;
            writer.StartBlock($"export interface {interfaceDeclaration.Name.ToFirstCharacterUpperCase()}{derivation} {{");
        }
    }
}
