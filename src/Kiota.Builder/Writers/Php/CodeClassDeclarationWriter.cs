using System;
using System.Linq;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Php
{
    public class CodeClassDeclarationWriter: BaseElementWriter<ClassDeclaration, PhpConventionService>
    {

        public CodeClassDeclarationWriter(PhpConventionService conventionService) : base(conventionService) { }

        public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(writer == null) throw new ArgumentNullException(nameof(writer));
            conventions.WritePhpDocumentStart(writer);
            conventions.WriteNamespaceAndImports(codeElement, writer);
            var derivation = (codeElement.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                                (!(codeElement.Implements.Any() && (codeElement.Inherits == null)) ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
            writer.WriteLine($"class {codeElement.Name.Split('.').Last().ToFirstCharacterUpperCase()}{derivation} ");

            writer.WriteLine("{");
            writer.IncreaseIndent();
        }
    }
}
