using System;
using System.Linq;
using System.Web;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

namespace Kiota.Builder.Writers.http;
public class CodeClassDeclarationWriter(HttpConventionService conventionService) : CodeProprietableBlockDeclarationWriter<ClassDeclaration>(conventionService)
{
    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is CodeClass requestBuilderClass && requestBuilderClass.IsOfKind(CodeClassKind.RequestBuilder))
        {
            // Write short description
            conventions.WriteShortDescription(requestBuilderClass, writer);
            writer.WriteLine();

            // Write the baseUrl variable
            WriteBaseUrl(requestBuilderClass, writer);

            // Extract and write the URL template
            WriteUrlTemplate(requestBuilderClass, writer);

            // Write path parameters
            WritePathParameters(requestBuilderClass, writer);

            // Write all query parameter variables
            WriteQueryParameters(requestBuilderClass, writer);

            // Write all HTTP methods GET, POST, PUT, DELETE e.t.c
            WriteHttpMethods(requestBuilderClass, writer);
        }
    }

    /// <summary>
    /// Writes the base URL for the given request builder class to the writer.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the base URL property.</param>
    /// <param name="writer">The language writer to write the base URL to.</param>
    private static void WriteBaseUrl(CodeClass requestBuilderClass, LanguageWriter writer)
    {
        // Retrieve the base URL property from the request builder class
        var baseUrl = requestBuilderClass.Properties
            .FirstOrDefault(property => property.Name.Equals("BaseUrl", StringComparison.OrdinalIgnoreCase))?.DefaultValue;

        // Write the base URL variable to the writer
        writer.WriteLine($"# baseUrl");
        writer.WriteLine($"@baseUrl = {baseUrl}");
        writer.WriteLine();
    }

    private static void WriteUrlTemplate(CodeClass requestBuilderClass, LanguageWriter writer)
    {
        var urlTemplateProperty = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .FirstOrDefault(property => property.IsOfKind(CodePropertyKind.UrlTemplate));

        var urlTemplate = urlTemplateProperty?.DefaultValue;
        writer.WriteLine($"# {urlTemplateProperty?.Documentation?.DescriptionTemplate}");
        writer.WriteLine($"# {urlTemplate}");
        writer.WriteLine();
    }

    /// <summary>
    /// Writes the path parameters for the given request builder class to the writer.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the path parameters.</param>
    /// <param name="writer">The language writer to write the path parameters to.</param>
    private static void WritePathParameters(CodeClass requestBuilderClass, LanguageWriter writer)
    {
        // Retrieve all the path variables except the generic path parameter named "pathParameters"
        var pathParameters = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .Where(property => property.IsOfKind(CodePropertyKind.PathParameters) && !property.Name.Equals("pathParameters", StringComparison.OrdinalIgnoreCase))
            .ToList();

        // Write each path parameter property
        pathParameters?.ForEach(prop =>
        {
            WriteHttpParameterProperty(prop, writer);
        });
    }

    /// <summary>
    /// Writes the query parameters for the given request builder class to the writer.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the query parameters.</param>
    /// <param name="writer">The language writer to write the query parameters to.</param>
    private static void WriteQueryParameters(CodeClass requestBuilderClass, LanguageWriter writer)
    {
        // Retrieve all the query parameter classes
        var queryParameterClasses = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeClass>()
            .Where(element => element.IsOfKind(CodeClassKind.QueryParameters))
            .ToList();

        // Write each query parameter property
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

    /// <summary>
    /// Writes the HTTP parameter property to the writer.
    /// </summary>
    /// <param name="property">The property to write.</param>
    /// <param name="writer">The language writer to write the property to.</param>
    private static void WriteHttpParameterProperty(CodeProperty property, LanguageWriter writer)
    {
        if (!string.IsNullOrEmpty(property.Name))
        {
            // Write the property documentation as a comment
            writer.WriteLine($"# {property.Documentation.DescriptionTemplate}");

            // Write the property name and an assignment placeholder
            writer.WriteLine($"@{property.Name.ToFirstCharacterLowerCase()} = ");

            // Write an empty line for separation
            writer.WriteLine();
        }
    }

    /// <summary>
    /// Writes the HTTP methods (GET, POST, PATCH, DELETE, e.t.c) for the given request builder class to the writer.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the HTTP methods.</param>
    /// <param name="writer">The language writer to write the HTTP methods to.</param>
    private static void WriteHttpMethods(CodeClass requestBuilderClass, LanguageWriter writer)
    {
        // Retrieve all the HTTP methods of kind RequestExecutor
        var httpMethods = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeMethod>()
            .Where(element => element.IsOfKind(CodeMethodKind.RequestExecutor))
            .ToList();

        // Write each HTTP method
        httpMethods?.ForEach(method =>
        {
            // Write the method documentation as a comment
            writer.WriteLine($"# {method.Documentation.DescriptionTemplate}");

            // Write the method name and URL template
            writer.WriteLine($"{method.Name.ToUpperInvariant()} {GetUrlTemplate(requestBuilderClass)}");

            // Write the request body if present
            WriteRequestBody(method, writer);

            // Write an empty line for separation
            writer.WriteLine();
            writer.WriteLine("###");
            writer.WriteLine();
        });
    }

    /// <summary>
    /// Retrieves the URL template for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the URL template property.</param>
    /// <returns>The URL template as a string, or an empty string if not found.</returns>
    private static string GetUrlTemplate(CodeClass requestBuilderClass)
    {
        // Retrieve the URL template property from the request builder class
        var urlTemplateProperty = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .FirstOrDefault(property => property.IsOfKind(CodePropertyKind.UrlTemplate));

        // Return the URL template or an empty string if not found
        return urlTemplateProperty?.DefaultValue ?? string.Empty;
    }

    /// <summary>
    /// Writes the request body for the given method to the writer.
    /// </summary>
    /// <param name="method">The method containing the request body.</param>
    /// <param name="writer">The language writer to write the request body to.</param>
    private static void WriteRequestBody(CodeMethod method, LanguageWriter writer)
    {
        // If there is a request body, write it
        var requestBody = method.Parameters.FirstOrDefault(param => param.IsOfKind(CodeParameterKind.RequestBody));
        if (requestBody is null) return;

        // Empty line before content type
        writer.WriteLine();
        writer.WriteLine($"Content-Type: {method.RequestBodyContentType}");

        // Loop through the properties of the request body and write a JSON object
        if (requestBody.Type is CodeType ct && ct.TypeDefinition is CodeClass requestBodyClass)
        {
            writer.WriteLine("{");
            writer.IncreaseIndent();
            WriteProperties(requestBodyClass, writer);
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
    }

    /// <summary>
    /// Writes the properties of the given request body class to the writer.
    /// </summary>
    /// <param name="requestBodyClass">The request body class containing the properties.</param>
    /// <param name="writer">The language writer to write the properties to.</param>
    private static void WriteProperties(CodeClass requestBodyClass, LanguageWriter writer)
    {
        var properties = requestBodyClass.Properties.Where(prop => prop.IsOfKind(CodePropertyKind.Custom)).ToList();
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
        if (requestBodyClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            WriteProperties(baseClass, writer);
        }
    }

    /// <summary>
    /// Gets the default value for the given property.
    /// </summary>
    /// <param name="codeProperty">The property to get the default value for.</param>
    /// <returns>The default value as a string.</returns>
    private static string GetDefaultValueForProperty(CodeProperty codeProperty)
    {
        return codeProperty.Type.Name switch
        {
            "int" or "integer" => "0",
            "string" => "\"string\"",
            "bool" or "boolean" => "false",
            _ when codeProperty.Type is CodeType enumType && enumType.TypeDefinition is CodeEnum enumDefinition =>
                enumDefinition.Options.FirstOrDefault()?.Name is string enumName ? $"\"{enumName}\"" : "null",
            _ => "null"
        };
    }
}
