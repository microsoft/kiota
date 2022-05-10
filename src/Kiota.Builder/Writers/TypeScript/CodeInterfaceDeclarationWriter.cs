
using System;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.TypeScript
{
    class CodeInterfaceWriter : BaseElementWriter<InterfaceDeclaration, TypeScriptConventionService>
    {
        private readonly CodeUsingWriter _codeUsingWriter;
        public CodeInterfaceWriter(TypeScriptConventionService conventionService, string clientNamespaceName) : base(conventionService)
        {
            _codeUsingWriter = new(clientNamespaceName);
        }


        /// <summary>
        /// Writes export statements for classes and enums belonging to a namespace into a generated index.ts file. 
        /// The classes should be export in the order of inheritance so as to avoid circular dependency issues in javascript.
        /// </summary>
        /// <param name="codeElement">Code element is a code namespace</param>
        /// <param name="writer"></param>
        public override void WriteCodeElement(InterfaceDeclaration interfaceDeclaration, LanguageWriter writer)
        {
            if (interfaceDeclaration == null) throw new ArgumentNullException(nameof(interfaceDeclaration));
            if (writer == null) throw new ArgumentNullException(nameof(writer));
            var parentNamespace = interfaceDeclaration.GetImmediateParentOfType<CodeNamespace>();
            _codeUsingWriter.WriteCodeElement(interfaceDeclaration.Usings, parentNamespace, writer);

            var inheritSymbol = "";
            foreach (var c in interfaceDeclaration?.inherits) {
                inheritSymbol = (!String.IsNullOrWhiteSpace(inheritSymbol) ? inheritSymbol + "," : String.Empty) + conventions.GetTypeString(c, interfaceDeclaration);
            }
            //var inheritSymbol = conventions.GetTypeString(, interfaceDeclaration);
            var derivation = (String.IsNullOrWhiteSpace(inheritSymbol) ? string.Empty : $" extends {inheritSymbol}");
            //  conventions.WriteShortDescription((codeInterface.Parent as CodeClass).Description, writer);

            writer.WriteLine($"export interface {interfaceDeclaration.Name.ToFirstCharacterUpperCase()}{derivation}{{");
            writer.IncreaseIndent();
        }
    }
}
