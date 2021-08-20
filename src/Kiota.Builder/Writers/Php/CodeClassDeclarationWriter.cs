using System;
using System.Linq;
using System.Reflection.Metadata;
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
            //TODO: There is bug on creating filenames that makes file class names have multiple dots.
            // for example class user.LoginRequestBuilder 
            // instead of classn LoginRequestBuilder {;

            if (codeElement != null)
            {
                var derivation = (codeElement?.Inherits == null ? string.Empty : $" extends {codeElement.Inherits.Name.ToFirstCharacterUpperCase()}") +
                                 (!codeElement.Implements.Any() ? string.Empty : $" implements {codeElement.Implements.Select(x => x.Name).Aggregate((x,y) => x + ", " + y)}");
                writer.WriteLine($"class {codeElement.Name.Split('.').Last().ToFirstCharacterUpperCase()}{derivation} ");
            }

            writer.WriteLine("{");
            writer.IncreaseIndent();
        }
    }
}
