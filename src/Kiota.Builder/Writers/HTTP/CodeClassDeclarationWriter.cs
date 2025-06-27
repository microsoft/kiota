using System;
using System.Collections.Generic;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;
using Microsoft.Kiota.Abstractions;
using Microsoft.OpenApi;

namespace Kiota.Builder.Writers.Http;

public class CodeClassDeclarationWriter(HttpConventionService conventionService) : CodeProprietableBlockDeclarationWriter<ClassDeclaration>(conventionService)
{
    private static class Constants
    {
        internal const string BaseUrlPropertyName = "hostAddress";
        internal const string PathParameters = "pathParameters";
        internal const string BaseUrl = "baseUrl";
        internal const string ApiKeyAuth = "apiKeyAuth";
        internal const string BearerAuth = "bearerAuth";
        internal const string HttpVersion = "HTTP/1.1";
        internal const string LocalHostUrl = "http://localhost/";

        internal static Dictionary<string, string> SchemeTypeMapping = new()
        {
            { SecuritySchemeType.ApiKey.ToString().ToLowerInvariant(), ApiKeyAuth },
            { SecuritySchemeType.Http.ToString().ToLowerInvariant(), BearerAuth },
            { SecuritySchemeType.OAuth2.ToString().ToLowerInvariant(), BearerAuth },
            { SecuritySchemeType.OpenIdConnect.ToString().ToLowerInvariant(), BearerAuth }
        };
    }

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

            var httpMethods = GetHttpMethods(requestBuilderClass);
            var methodQueriesAndParameters = new Dictionary<CodeMethod, List<CodeProperty>>();

            // Write all query parameter variables
            WriteQueryParameters(queryParameters, writer);

            // Write path parameters
            WritePathParameters(pathParameters, writer);

            foreach (var method in httpMethods)
            {
                var builderClassName = requestBuilderClass.Name;
                foreach (var queryParameter in queryParameters)
                {
                    var parentClassName = queryParameter.Parent?.Name;
                    if (parentClassName is not null && parentClassName.Contains(builderClassName + method.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!methodQueriesAndParameters.TryGetValue(method, out var value))
                        {
                            value = [];
                            methodQueriesAndParameters[method] = value;
                        }

                        value.Add(queryParameter);
                    }
                }
            }

            if (methodQueriesAndParameters.Count > 0)
            {
                foreach (var (method, parameters) in methodQueriesAndParameters)
                {
                    // Write the HTTP methods
                    WriteHttpMethods(requestBuilderClass, writer, [.. parameters], pathParameters, urlTemplateProperty, method, baseUrl);
                }
            }
            else
            {
                // Write the HTTP methods without query parameters
                foreach (var method in httpMethods)
                {
                    WriteHttpMethods(requestBuilderClass, writer, [], pathParameters, urlTemplateProperty, method, baseUrl);
                }
            }

        }
    }

    /// <summary>
    /// Retrieves all the query parameters for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the query parameters.</param>
    /// <returns>An array of all query parameters.</returns>
    private static CodeProperty[] GetAllQueryParameters(CodeClass requestBuilderClass)
    {
        // Retrieve all the query parameter classes
        return requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeClass>()
            .Where(static element => element.IsOfKind(CodeClassKind.QueryParameters))
            .SelectMany(paramCodeClass => paramCodeClass.Properties)
            .Where(static property => property.IsOfKind(CodePropertyKind.QueryParameter))
            .ToArray();
    }

    /// <summary>
    /// Retrieves all the path parameters for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the path parameters.</param>
    /// <returns>An array of all path parameters, or an empty array if none are found.</returns>
    private static CodeProperty[] GetPathParameters(CodeClass requestBuilderClass)
    {
        // Retrieve all the path variables except the generic path parameter named "pathParameters"
        var pathParameters = requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeProperty>()
            .Where(property => property.IsOfKind(CodePropertyKind.PathParameters) && !property.Name.Equals(Constants.PathParameters, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return pathParameters;
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
            .FirstOrDefault(property => property.Name.Equals(Constants.BaseUrl, StringComparison.OrdinalIgnoreCase))?.DefaultValue;
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
    /// Writes the path parameters for the given request builder class to the writer.
    /// </summary>
    /// <param name="pathParameters">The array of path parameters to write.</param>
    /// <param name="writer">The language writer to write the path parameters to.</param>
    private static void WritePathParameters(CodeProperty[] pathParameters, LanguageWriter writer)
    {
        var uniquePathParameters = pathParameters
            .GroupBy(static param => param.Name)
            .Select(static group => group.First())
            .ToArray();
        // Write each path parameter property
        foreach (var pathParameter in uniquePathParameters)
        {
            WriteHttpParameterProperty(pathParameter, writer);
        }
    }

    /// <summary>
    /// Writes the query parameters for the given request builder class to the writer.
    /// </summary>
    /// <param name="queryParameters">The array of query parameters to write.</param>
    /// <param name="writer">The language writer to write the query parameters to.</param>
    private static void WriteQueryParameters(CodeProperty[] queryParameters, LanguageWriter writer)
    {
        var uniqueQueryParameters = queryParameters
            .GroupBy(static param => param.Name)
            .Select(static group => group.First())
            .ToArray();
        // Write each query parameter property
        foreach (var queryParameter in uniqueQueryParameters)
        {
            WriteHttpParameterProperty(queryParameter, writer);
        }
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
    /// <param name="queryParameters">The array of query parameters.</param>
    /// <param name="pathParameters">The array of path parameters.</param>
    /// <param name="urlTemplateProperty">The URL template property containing the URL template.</param>
    /// <param name="baseUrl">The base URL.</param>
    private static void WriteHttpMethods(
        CodeClass requestBuilderClass,
        LanguageWriter writer,
        CodeProperty[] queryParameters,
        CodeProperty[] pathParameters,
        CodeProperty urlTemplateProperty,
        CodeMethod method,
        string? baseUrl)
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
        writer.WriteLine($"{method.Name.ToUpperInvariant()} {url} {Constants.HttpVersion}");

        var authenticationMethod = requestBuilderClass
            .Properties
            .FirstOrDefault(static prop => prop.IsOfKind(CodePropertyKind.Headers));

        if (authenticationMethod != null
            && Enum.TryParse(typeof(SecuritySchemeType), authenticationMethod.Type.Name, true, out var schemeTypeObj)
            && schemeTypeObj is SecuritySchemeType schemeType
            && Constants.SchemeTypeMapping.TryGetValue(schemeType.ToString().ToLowerInvariant(), out var mappedSchemeType))
        {
            writer.WriteLine($"Authorization: {{{{{mappedSchemeType}}}}}");
        }

        // Write the request body if present
        WriteRequestBody(method, writer);

        writer.WriteLine();
        writer.WriteLine("###");
        writer.WriteLine();
    }

    /// <summary>
    /// Retrieves all the HTTP methods of kind RequestExecutor for the given request builder class.
    /// </summary>
    /// <param name="requestBuilderClass">The request builder class containing the HTTP methods.</param>
    /// <returns>An array of HTTP methods of kind RequestExecutor.</returns>
    private static CodeMethod[] GetHttpMethods(CodeClass requestBuilderClass)
    {
        return [.. requestBuilderClass
            .GetChildElements(true)
            .OfType<CodeMethod>()
            .Where(static element => element.IsOfKind(CodeMethodKind.RequestExecutor))];
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
    private static void WriteProperties(CodeClass requestBodyClass, LanguageWriter writer, HashSet<CodeClass>? processedClasses = null, int depth = 0)
    {

        processedClasses ??= [];

        // Add the current class to the set of processed classes
        if (!processedClasses.Add(requestBodyClass))
        {
            // If the class is already processed, write its properties again up to a certain depth
            if (depth >= 3)
            {
                return;
            }
        }

        var properties = requestBodyClass.Properties
            .Where(static prop => prop.IsOfKind(CodePropertyKind.Custom))
            .ToArray();

        foreach (var prop in properties)
        {
            // Add a trailing comma if there are more properties to be written
            var separator = ",";
            var propName = $"\"{prop.Name}\"";
            writer.Write($"{propName}: ");

            if (prop.Type is CodeType propType && propType.TypeDefinition is CodeClass propClass)
            {
                // If the property is an object, write a JSON representation recursively
                writer.WriteLine("{", includeIndent: false);
                writer.IncreaseIndent();
                WriteProperties(propClass, writer, processedClasses, depth + 1);
                writer.CloseBlock($"}}{separator}");
            }
            else
            {
                writer.WriteLine($"{HttpConventionService.GetDefaultValueForProperty(prop)}{separator}", includeIndent: false);
            }
        }

        // Remove the current class from the set of processed classes after processing
        processedClasses.Remove(requestBodyClass);

        // If the class extends another class, write properties of the base class
        if (requestBodyClass.StartBlock.Inherits?.TypeDefinition is CodeClass baseClass)
        {
            WriteProperties(baseClass, writer, processedClasses, depth + 1);
        }
    }

    private static string BuildUrlStringFromTemplate(string urlTemplateString, CodeProperty[] queryParameters, CodeProperty[] pathParameters, string? baseUrl)
    {
        // Use the provided baseUrl or default to "http://localhost/"
        baseUrl ??= Constants.LocalHostUrl;

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
        return requestInformation.URI.ToString().Replace(baseUrl, $"{{{{{Constants.BaseUrlPropertyName}}}}}", StringComparison.InvariantCultureIgnoreCase);
    }
}
