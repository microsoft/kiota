using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Microsoft.Kiota.Abstractions;

namespace Kiota.Builder.Writers.Http;
public class CodeClassDeclarationWriter(HttpConventionService conventionService) : CodeProprietableBlockDeclarationWriter<ClassDeclaration>(conventionService)
{
    private const string BaseUrlPropertyName = "url";

    protected override void WriteTypeDeclaration(ClassDeclaration codeElement, LanguageWriter writer)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(writer);

        if (codeElement.Parent is CodeClass requestBuilderClass && requestBuilderClass.IsOfKind(CodeClassKind.RequestBuilder) && GetUrlTemplateProperty(requestBuilderClass) is CodeProperty urlTemplateProperty)
        {
            // Write short description
            conventions.WriteShortDescription(requestBuilderClass, writer);
            writer.WriteLine();

            // Retrieve all query parameters
            var queryParameters = GetAllQueryParameters(requestBuilderClass);

            // Retrieve all path parameters
            var pathParameters = GetPathParameters(requestBuilderClass);

            var baseUrl = GetBaseUrl(requestBuilderClass);

            // Write the baseUrl variable
            WriteBaseUrl(baseUrl, writer);

            // Extract and write the URL template
            WriteUrlTemplate(urlTemplateProperty, writer);

            // Write path parameters
            WritePathParameters(pathParameters, writer);

            // Write all query parameter variables
            WriteQueryParameters(queryParameters, writer);

            // Write all HTTP methods GET, POST, PUT, DELETE e.t.c
            WriteHttpMethods(requestBuilderClass, writer, queryParameters, pathParameters, urlTemplateProperty, baseUrl);
        }
    }

    /// <summary>
    /// Retrieves all the query parameters for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the query parameters.</param>
    /// <returns>A list of all query parameters.</returns>
    private static List<CodeProperty> GetAllQueryParameters(CodeClass requestBuilderClass)
    {
        var queryParameters = new List<CodeProperty>();

        // Retrieve all the query parameter classes
        var queryParameterClasses = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeClass>()
            .Where(static element => element.IsOfKind(CodeClassKind.QueryParameters))
            .ToList();

        // Collect all query parameter properties into the aggregated list
        queryParameterClasses?.ForEach(paramCodeClass =>
        {
            var queryParams = paramCodeClass
                .Properties
                .Where(static property => property.IsOfKind(CodePropertyKind.QueryParameter))
                .ToList();

            queryParameters.AddRange(queryParams);
        });

        return queryParameters;
    }

    /// <summary>
    /// Retrieves all the path parameters for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the path parameters.</param>
    /// <returns>A list of all path parameters, or an empty list if none are found.</returns>
    private static List<CodeProperty> GetPathParameters(CodeClass requestBuilderClass)
    {
        // Retrieve all the path variables except the generic path parameter named "pathParameters"
        var pathParameters = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .Where(property => property.IsOfKind(CodePropertyKind.PathParameters) && !property.Name.Equals("pathParameters", StringComparison.OrdinalIgnoreCase))
            .ToList();

        return pathParameters;
    }

    /// <summary>
    /// Writes the base URL for the given request builder class to the writer.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the base URL property.</param>
    /// <param name="writer">The language writer to write the base URL to.</param>
    private static void WriteBaseUrl(string? baseUrl, LanguageWriter writer)
    {
        // Write the base URL variable to the writer
        writer.WriteLine($"# Base url for the server/host");
        writer.WriteLine($"@{BaseUrlPropertyName} = {baseUrl}");
        writer.WriteLine();
    }

    /// <summary>
    /// Retrieves the base URL for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the base URL property.</param>
    /// <returns>The base URL as a string, or null if not found.</returns>
    private static string? GetBaseUrl(CodeClass requestBuilderClass)
    {
        // Retrieve the base URL property from the request builder class
        return requestBuilderClass.Properties
            .FirstOrDefault(property => property.Name.Equals("BaseUrl", StringComparison.OrdinalIgnoreCase))?.DefaultValue;
    }

    /// <summary>
    /// Retrieves the URL template property for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the URL template property.</param>
    /// <returns>The URL template property, or null if not found.</returns>
    private static CodeProperty? GetUrlTemplateProperty(CodeClass requestBuilderClass)
    {
        // Retrieve the URL template property from the request builder class
        return requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .FirstOrDefault(static property => property.IsOfKind(CodePropertyKind.UrlTemplate));
    }

    /// <summary>
    /// Writes the URL template for the given URL template property to the writer.
    /// </summary>
    /// <param name="urlTemplateProperty">The URL template property containing the URL template.</param>
    /// <param name="writer">The language writer to write the URL template to.</param>
    private static void WriteUrlTemplate(CodeProperty urlTemplateProperty, LanguageWriter writer)
    {
        // Write the URL template documentation as a comment
        writer.WriteLine($"# {urlTemplateProperty.Documentation?.DescriptionTemplate}");

        // Write the URL template value
        writer.WriteLine($"# {urlTemplateProperty.DefaultValue}");

        // Write an empty line for separation
        writer.WriteLine();
    }

    /// <summary>
    /// Writes the path parameters for the given request builder class to the writer.
    /// </summary>
    /// <param name="pathParameters">The list of path parameters to write.</param>
    /// <param name="writer">The language writer to write the path parameters to.</param>
    private static void WritePathParameters(List<CodeProperty> pathParameters, LanguageWriter writer)
    {
        // Write each path parameter property
        pathParameters?.ForEach(prop => WriteHttpParameterProperty(prop, writer));
    }

    /// <summary>
    /// Writes the query parameters for the given request builder class to the writer.
    /// </summary>
    /// <param name="queryParameters">The list of query parameters to write.</param>
    /// <param name="writer">The language writer to write the query parameters to.</param>
    private static void WriteQueryParameters(List<CodeProperty> queryParameters, LanguageWriter writer)
    {
        // Write each query parameter property
        queryParameters.ForEach(prop => WriteHttpParameterProperty(prop, writer));
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
    /// Writes the HTTP methods (GET, POST, PATCH, DELETE, etc.) for the given request builder class to the writer.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the HTTP methods.</param>
    /// <param name="writer">The language writer to write the HTTP methods to.</param>
    /// <param name="queryParameters">The list of query parameters.</param>
    /// <param name="pathParameters">The list of path parameters.</param>
    /// <param name="urlTemplateProperty">The URL template property containing the URL template.</param>
    /// <param name="baseUrl">The base URL.</param>
    private static void WriteHttpMethods(
        CodeClass requestBuilderClass,
        LanguageWriter writer,
        List<CodeProperty> queryParameters,
        List<CodeProperty> pathParameters,
        CodeProperty urlTemplateProperty,
        string? baseUrl)
    {
        // Retrieve all the HTTP methods of kind RequestExecutor
        var httpMethods = GetHttpMethods(requestBuilderClass);

        var methodCount = httpMethods.Count;
        var currentIndex = 0;

        foreach (var method in httpMethods)
        {
            // Write the method documentation as a comment
            writer.WriteLine($"# {method.Documentation.DescriptionTemplate}");

            // Build the actual URL string and replace all required fields (path and query) with placeholder variables
            var url = BuildUrlStringFromTemplate(
                urlTemplateProperty.DefaultValue,
                queryParameters,
                pathParameters,
                baseUrl
            );

            // Write the HTTP operation (e.g., GET, POST, PATCH, etc.)
            writer.WriteLine($"{method.Name.ToUpperInvariant()} {url} HTTP/1.1");

            // Write the request body if present
            WriteRequestBody(method, writer);

            // Write a separator if there are more items that follow
            if (++currentIndex < methodCount)
            {
                writer.WriteLine();
                writer.WriteLine("###");
                writer.WriteLine();
            }
        }
    }

    /// <summary>
    /// Retrieves all the HTTP methods of kind RequestExecutor for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the HTTP methods.</param>
    /// <returns>A list of HTTP methods of kind RequestExecutor.</returns>
    private static List<CodeMethod> GetHttpMethods(CodeClass requestBuilderClass)
    {
        return requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeMethod>()
            .Where(static element => element.IsOfKind(CodeMethodKind.RequestExecutor))
            .ToList();
    }

    /// <summary>
    /// Writes the request body for the given method to the writer.
    /// </summary>
    /// <param name="method">The method containing the request body.</param>
    /// <param name="writer">The language writer to write the request body to.</param>
    private static void WriteRequestBody(CodeMethod method, LanguageWriter writer)
    {
        // If there is a request body, write it
        var requestBody = method.Parameters.FirstOrDefault(static param => param.IsOfKind(CodeParameterKind.RequestBody));
        if (requestBody is null) return;

        writer.WriteLine($"Content-Type: {method.RequestBodyContentType}");

        // Empty line before body content
        writer.WriteLine();

        // Loop through the properties of the request body and write a JSON object
        if (requestBody.Type is CodeType ct && ct.TypeDefinition is CodeClass requestBodyClass)
        {
            writer.StartBlock();
            WriteProperties(requestBodyClass, writer);
            writer.CloseBlock();
        }
    }

    /// <summary>
    /// Writes the properties of the given request body class to the writer.
    /// </summary>
    /// <param name="requestBodyClass">The request body class containing the properties.</param>
    /// <param name="writer">The language writer to write the properties to.</param>
    private static void WriteProperties(CodeClass requestBodyClass, LanguageWriter writer)
    {
        var properties = requestBodyClass.Properties
            .Where(static prop => prop.IsOfKind(CodePropertyKind.Custom))
            .ToArray();

        var propertyCount = properties.Length;
        var currentIndex = 0;

        foreach (var prop in properties)
        {
            // Add a trailing comma if there are more properties to be written
            var separator = currentIndex < propertyCount - 1 ? "," : string.Empty;
            var propName = $"\"{prop.Name}\"";
            writer.Write($"{propName}: ");

            if (prop.Type is CodeType propType && propType.TypeDefinition is CodeClass propClass)
            {
                // If the property is an object, write a JSON representation recursively
                writer.WriteLine("{", includeIndent: false);
                writer.IncreaseIndent();
                WriteProperties(propClass, writer);
                writer.CloseBlock($"}}{separator}");
            }
            else
            {
                writer.WriteLine($"{HttpConventionService.GetDefaultValueForProperty(prop)}{separator}", includeIndent: false);
            }
        }

        // If the class extends another class, write properties of the base class
        if (requestBodyClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            WriteProperties(baseClass, writer);
        }
    }

    private static string BuildUrlStringFromTemplate(string urlTemplateString, List<CodeProperty> queryParameters, List<CodeProperty> pathParameters, string? baseUrl)
    {
        // Use the provided baseUrl or default to "http://localhost/"
        baseUrl ??= "http://localhost/";

        // unquote the urlTemplate string and replace the {+baseurl} with the actual base url string
        urlTemplateString = urlTemplateString.Trim('"').Replace("{+baseurl}", baseUrl, StringComparison.InvariantCultureIgnoreCase);

        // Build RequestInformation using the URL
        var requestInformation = new RequestInformation()
        {
            UrlTemplate = urlTemplateString,
            QueryParameters = queryParameters.ToDictionary(item => item.WireName, item => $"{{{{{item.Name.ToFirstCharacterLowerCase()}}}}}" as object),
            PathParameters = pathParameters.ToDictionary(item => item.WireName, item => $"{{{{{item.Name.ToFirstCharacterLowerCase()}}}}}" as object),
        };

        // Erase baseUrl and use the placeholder variable {baseUrl} already defined in the snippet
        return requestInformation.URI.ToString().Replace(baseUrl, $"{{{{{BaseUrlPropertyName}}}}}", StringComparison.InvariantCultureIgnoreCase);
    }
}
