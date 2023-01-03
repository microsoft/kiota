using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.Markdown
{
    public class CodeClassDeclarationWriter : BaseElementWriter<ClassDeclaration, MarkdownConventionService>
    {
        public CodeClassDeclarationWriter(MarkdownConventionService conventionService): base(conventionService) { }
        public override void WriteCodeElement(ClassDeclaration codeElement, LanguageWriter writer)
        {
            if(codeElement == null) throw new ArgumentNullException(nameof(codeElement));
            if(writer == null) throw new ArgumentNullException(nameof(writer));

            var parentClass = codeElement.Parent as CodeClass;
            var urlTemplateProp = parentClass.GetPropertyOfKind(CodePropertyKind.UrlTemplate); 
            // Switch on the parent class kind
            switch(parentClass.Kind) {
                case CodeClassKind.Model:
                    writer.WriteLine($"## {parentClass.Name}");
                    writer.WriteLine();
                    conventions.WriteLongDescription(parentClass.Documentation, writer);
                    writer.WriteLine("|Name| Type| Default | Description|");
                    writer.WriteLine("|---|---|---|---|");
                    break;
                case CodeClassKind.RequestBuilder:
                    writer.WriteLine($"## {urlTemplateProp.DefaultValue}");
                    writer.WriteLine();
                    conventions.WriteLongDescription(parentClass.Documentation, writer);
                    writer.WriteLine();
                    writer.WriteLine("|Relation| Description|");
                    writer.WriteLine("|---|---|");
                    break;
                case CodeClassKind.RequestConfiguration:
                    break;
                case CodeClassKind.QueryParameters:
                    writer.WriteLine("### Query Parameters");
                    writer.WriteLine("|Name| Type| Default | Description|");
                    writer.WriteLine("|---|---|---|---|");
                    break;

                default:
                    break;
            }


        }

    }
}
