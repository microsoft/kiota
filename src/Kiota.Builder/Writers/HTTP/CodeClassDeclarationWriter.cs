using System;
using System.Linq;

using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.http;
public class CodeClassDeclarationWriter(HttpConventionService conventionService) : CodeProprietableBlockDeclarationWriter<ClassDeclaration>(conventionService)
{
    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteLine();

        if (codeElement.Parent is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            conventions.WriteShortDescription(codeClass, writer);
            // its a request builder class

            // TODO: write the baseUrl variable e.g @baseUrl = http://loccalhost:3000

            // extract the URL Template
            var urlTemplateProperty = codeElement.Parent
                .GetChildElements(true)
                .OfType<CodeProperty>()
                .FirstOrDefault(property => property.IsOfKind(CodePropertyKind.UrlTemplate));

            var urlTemplate = urlTemplateProperty?.DefaultValue;
            // Write the URL template comment
            writer.WriteLine($"# {urlTemplateProperty?.Documentation?.DescriptionTemplate}");
            writer.WriteLine($"# {urlTemplate}");
            writer.WriteLine();

            // TODO: write path variables e.g @post-id = 

            // Write all the query parameter variables
            var queryParameterClasses = codeElement.Parent?
                .GetChildElements(true)
                .OfType<CodeClass>()
                .Where(element => element.IsOfKind(CodeClassKind.QueryParameters))
                .ToList();
            queryParameterClasses?.ForEach(paramCodeClass =>
            {
                // Write all query parameters
                // Select all properties of type query parameters 
                var queryParams = paramCodeClass
                    .Properties
                    .Where(property => property.IsOfKind(CodePropertyKind.QueryParameter))
                    .ToList();

                queryParams.ForEach(prop => {
                    // Write the documentation
                    var documentation = prop.Documentation.DescriptionTemplate;
                    writer.WriteLine($"# {documentation}");
                    writer.WriteLine($"@{prop.Name} = ");
                    writer.WriteLine();
                });
            });

            // Write all http methods
            var httpMethods = codeElement.Parent?
                .GetChildElements(true)
                .OfType<CodeMethod>()
                .Where(element => element.IsOfKind(CodeMethodKind.RequestExecutor))
                .ToList();
            httpMethods?.ForEach(method =>
            {
                // Write http operations e.g GET, POST, DELETE e.t.c
                // Get the documentation
                var documentation = method.Documentation.DescriptionTemplate;
                writer.WriteLine($"# {documentation}");
                writer.WriteLine($"{method.Name.ToUpperInvariant()} {urlTemplate}");
                writer.WriteLine("###");
                writer.WriteLine("");
            });
        }
    }
}
