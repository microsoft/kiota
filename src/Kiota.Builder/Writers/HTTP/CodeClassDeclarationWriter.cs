using System;
using System.Linq;
using System.Web;
using Kiota.Builder.CodeDOM;

namespace Kiota.Builder.Writers.http;
public class CodeClassDeclarationWriter(HttpConventionService conventionService) : CodeProprietableBlockDeclarationWriter<ClassDeclaration>(conventionService)
{
    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is CodeClass codeClass && codeClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            // Write short description
            conventions.WriteShortDescription(codeClass, writer);
            writer.WriteLine();

            // Write the baseUrl variable
            WriteBaseUrl(codeClass, writer);

            // Extract and write the URL template
            WriteUrlTemplate(codeElement, writer);

            // Write path parameters
            WritePathParameters(codeElement, writer);

            // Write all query parameter variables
            WriteQueryParameters(codeElement, writer);

            // Write all HTTP methods GET, POST, PUT, DELETE e.t.c
            WriteHttpMethods(codeElement, writer);
        }
    }

    private static void WriteBaseUrl(CodeClass codeClass, LanguageWriter writer)
    {
        var baseUrl = codeClass.Properties.FirstOrDefault(property => property.Name.Equals("BaseUrl", StringComparison.OrdinalIgnoreCase))?.DefaultValue;
        writer.WriteLine($"# baseUrl");
        writer.WriteLine($"@baseUrl = {baseUrl}");
        writer.WriteLine();
    }

    private static void WriteUrlTemplate(CodeElement codeElement, LanguageWriter writer)
    {
        var urlTemplateProperty = codeElement.Parent?
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .FirstOrDefault(property => property.IsOfKind(CodePropertyKind.UrlTemplate));

        var urlTemplate = urlTemplateProperty?.DefaultValue;
        writer.WriteLine($"# {urlTemplateProperty?.Documentation?.DescriptionTemplate}");
        writer.WriteLine($"# {urlTemplate}");
        writer.WriteLine();
    }

    private static void WritePathParameters(CodeElement codeElement, LanguageWriter writer)
    {
        var pathParameters = codeElement.Parent?
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .Where(property => property.IsOfKind(CodePropertyKind.PathParameters))
            .ToList();

        pathParameters?.ForEach(prop =>
        {
            WriteHttpParameterProperty(prop, writer);
        });
    }

    private static void WriteQueryParameters(CodeElement codeElement, LanguageWriter writer)
    {
        var queryParameterClasses = codeElement.Parent?
            .GetChildElements(true)
            .OfType<CodeClass>()
            .Where(element => element.IsOfKind(CodeClassKind.QueryParameters))
            .ToList();

        queryParameterClasses?.ForEach(paramCodeClass =>
        {
            var queryParams = paramCodeClass
                .Properties
                .Where(property => property.IsOfKind(CodePropertyKind.QueryParameter))
                .ToList();

            queryParams.ForEach(prop =>
            {
                WriteHttpParameterProperty(prop, writer);
            });
        });
    }

    private static void WriteHttpParameterProperty(CodeProperty property, LanguageWriter writer)
    {
        if (!string.IsNullOrEmpty(property.Name))
        {
            writer.WriteLine($"# {property.Documentation.DescriptionTemplate}");
            writer.WriteLine($"@{property.Name} = ");
            writer.WriteLine();
        }
    }

    private static void WriteHttpMethods(CodeElement codeElement, LanguageWriter writer)
    {
        var httpMethods = codeElement.Parent?
            .GetChildElements(true)
            .OfType<CodeMethod>()
            .Where(element => element.IsOfKind(CodeMethodKind.RequestExecutor))
            .ToList();

        httpMethods?.ForEach(method =>
        {
            writer.WriteLine($"# {method.Documentation.DescriptionTemplate}");
            writer.WriteLine($"{method.Name.ToUpperInvariant()} {GetUrlTemplate(codeElement)}");

            WriteRequestBody(method, writer);

            writer.WriteLine();
            writer.WriteLine("###");
            writer.WriteLine();
        });
    }

    private static string GetUrlTemplate(CodeElement codeElement)
    {
        var urlTemplateProperty = codeElement.Parent?
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .FirstOrDefault(property => property.IsOfKind(CodePropertyKind.UrlTemplate));

        return urlTemplateProperty?.DefaultValue ?? string.Empty;
    }

    private static void WriteRequestBody(CodeMethod method, LanguageWriter writer)
    {
        // If there is a request body, write it
        var requestBody = method.Parameters.FirstOrDefault(param => param.IsOfKind(CodeParameterKind.RequestBody));
        if (requestBody is null) return;

        // Empty line before content type
        writer.WriteLine();
        writer.WriteLine($"Content-Type: {method.RequestBodyContentType}");

        // loop through the properties of the request body and write a JSON object
        if (requestBody.Type is CodeType ct && ct.TypeDefinition is CodeClass requestBodyClass)
        {
            writer.WriteLine("{");
            writer.IncreaseIndent();
            WriteProperties(requestBodyClass, writer);
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }
    private static void WriteProperties(CodeClass codeClass, LanguageWriter writer)
    {
        var properties = codeClass.Properties.Where(prop => prop.IsOfKind(CodePropertyKind.Custom)).ToList();
        for (int i = 0; i < properties.Count; i++)
        {
            var prop = properties[i];
            var propName = $"\"{prop.Name}\"";
            writer.Write($"{propName}: ");
            if (prop.Type is CodeType propType && propType.TypeDefinition is CodeClass propClass)
            {
                // If the property is an object, write a JSON representation recursively
                writer.WriteLine("{", includeIndent: false);
                writer.IncreaseIndent();
                WriteProperties(propClass, writer);
                writer.DecreaseIndent();
                writer.Write("}");
            }
            else
            {
                writer.Write(GetDefaultValueForProperty(prop), includeIndent: false);
            }

            // Add a trailing comma if there are more properties to be written
            if (i < properties.Count - 1)
            {
                writer.WriteLine(",", includeIndent: false);
            }
            else
            {
                writer.WriteLine();
            }
        }

        // If the class extends another class, write properties of the base class
        if (codeClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            WriteProperties(baseClass, writer);
        }
    }

    private static string GetDefaultValueForProperty(CodeProperty prop)
    {
        return prop.Type.Name switch
        {
            "int" or "integer" => "0",
            "string" => "\"string\"",
            "bool" or "boolean" => "false",
            _ when prop.Type is CodeType enumType && enumType.TypeDefinition is CodeEnum enumDefinition =>
                enumDefinition.Options.FirstOrDefault()?.Name is string enumName ? $"\"{enumName}\"" : "null",
            _ => "null"
        };
    }
}
