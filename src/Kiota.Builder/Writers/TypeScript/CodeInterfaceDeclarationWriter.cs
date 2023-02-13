
using System;
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
            if (interfaceDeclaration == null) throw new ArgumentNullException(nameof(interfaceDeclaration));
            ArgumentNullException.ThrowIfNull(writer);
            var parentNamespace = interfaceDeclaration.GetImmediateParentOfType<CodeNamespace>();
            _codeUsingWriter.WriteCodeElement(interfaceDeclaration.Usings, parentNamespace, writer);

            var inheritSymbol = "";
            foreach (var inherit in interfaceDeclaration.Implements)
            {
                var name = conventions.GetTypeString(inherit, interfaceDeclaration);
                inheritSymbol = (!String.IsNullOrWhiteSpace(inheritSymbol) ? inheritSymbol + ", " : String.Empty) + name;
            }

            var derivation = (String.IsNullOrWhiteSpace(inheritSymbol) ? string.Empty : $" extends {inheritSymbol}");

            writer.StartBlock($"export interface {interfaceDeclaration.Name.ToFirstCharacterUpperCase()}{derivation} {{");
        }
    }
}
