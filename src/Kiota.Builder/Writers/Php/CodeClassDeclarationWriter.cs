using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeClassDeclarationWriter: BaseElementWriter<CodeClass.Declaration, PhpConventionService>
    {

        public CodeClassDeclarationWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(CodeClass.Declaration codeElement, LanguageWriter writer)
        {
            conventions.WritePhpDocumentStart(writer);
            conventions.WriteNamespaceAndImports(codeElement, writer);
            if (codeElement != null)
            {
                var derivation = (codeElement?.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                                 (!(codeElement.Implements.Any() && (codeElement.Inherits == null)) ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
                writer.WriteLine($"class {codeElement.Name.Split('.').Last().ToFirstCharacterUpperCase()}{derivation} ");
            }

            writer.WriteLine("{");
            writer.IncreaseIndent();
        }
    }
}
