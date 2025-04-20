using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Kiota.Builder.CodeDOM;
using static Kiota.Builder.CodeDOM.CodeTypeBase;

namespace Kiota.Builder.Writers.AL;

/// <summary>
/// This is a collection of extension methods for the IDocumentedElement interface.
/// It provides methods to set, get, and remove custom properties from the documentation of the element.
/// It also provides helper methods to check the type of the element (e.g., if it's a parameter, variable, or object property).
/// These methods are used as a workaround, to avoid modifying the Kiota code generation library. Instead we just add some information here.
/// This might be a temporary solution, until we have a better way to handle this. Also, the added information are removed before the code is written.
/// </summary>
public static class CustomPropertyExtensions
{
    public static void SetCustomProperties(this IDocumentedElement element, Dictionary<string, string> customProperties)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(customProperties);
        foreach (var property in customProperties)
            element.AddCustomProperty(property.Key, property.Value);
        if (!element.Documentation.DescriptionTemplate.Contains("||", StringComparison.OrdinalIgnoreCase))
            element.Documentation.DescriptionTemplate += "||";
    }
    public static void RemoveCustomProperty(this IDocumentedElement codeElement, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(propertyName);
        if (codeElement.Documentation is null)
            return;
        var properties = GetCustomProperties(codeElement.Documentation);
        RemoveCustomProperties(codeElement);
        properties.Remove(propertyName);
        codeElement.SetCustomProperties(properties);
    }

    public static void RemoveCustomProperties(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        if (element.Documentation is null)
            return;
        if (!element.Documentation.DescriptionTemplate.Contains("||", StringComparison.OrdinalIgnoreCase))
            return;
        element.Documentation.DescriptionTemplate = element.Documentation.DescriptionTemplate.Substring(0, element.Documentation.DescriptionTemplate.IndexOf("||", StringComparison.OrdinalIgnoreCase));
    }
    public static void AddCustomProperty(this IDocumentedElement element, string propertyName, string propertyValue)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(propertyName);
        ArgumentNullException.ThrowIfNull(propertyValue);
        if (element.Documentation is null)
            element.Documentation = new CodeDocumentation();
        if (element.Documentation.DescriptionTemplate is null)
            element.Documentation.DescriptionTemplate = string.Empty;
        if (!element.Documentation.DescriptionTemplate.Contains("||", StringComparison.OrdinalIgnoreCase))
            element.Documentation.DescriptionTemplate += "||";
        if (!String.IsNullOrEmpty(element.GetCustomProperty(propertyName)))
            element.RemoveCustomProperty(propertyName);
        element.Documentation.DescriptionTemplate += $"{propertyName}={propertyValue};";
    }
    public static string GetCustomProperty(this IDocumentedElement codeElement, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(codeElement);
        ArgumentNullException.ThrowIfNull(propertyName);
        if (codeElement.Documentation is null)
            return string.Empty;
        return GetCustomProperty(codeElement.Documentation, propertyName);
    }
    private static Dictionary<string, string> GetCustomProperties(CodeDocumentation documentation)
    {
        ArgumentNullException.ThrowIfNull(documentation);
        if (documentation.DescriptionTemplate is null)
            return new Dictionary<string, string>();
        var baseString = documentation.DescriptionTemplate;
        if (baseString.Contains("||", StringComparison.OrdinalIgnoreCase))
            baseString = baseString.Remove(0, baseString.IndexOf("||", StringComparison.OrdinalIgnoreCase) + 2);
        var entries = baseString.Split(";", StringSplitOptions.RemoveEmptyEntries);
        var customProperties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            var parts = entry.Split("=", StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
                customProperties.Add(parts[0], parts[1]);
        }
        return customProperties;
    }
    private static string GetCustomProperty(CodeDocumentation documentation, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(documentation);
        ArgumentNullException.ThrowIfNull(propertyName);
        var customProperties = GetCustomProperties(documentation);
        if (customProperties.TryGetValue(propertyName, out var value))
            return value;
        return string.Empty;
    }
    public static Dictionary<string, string> GetCustomProperties(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        var customProperties = new Dictionary<string, string>();
        foreach (var property in GetCustomProperties(element.Documentation))
        {
            if (!customProperties.ContainsKey(property.Key))
                customProperties.Add(property.Key, property.Value);
        }
        return customProperties;
    }
    public static string GetSource(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetCustomProperty("source");
    }
    public static string GetSourceType(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetCustomProperty("source-type");
    }
    public static string GetPragmas(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return element.GetCustomProperty("pragmas");
    }
    public static string GetPragmasVariables(this IDocumentedElement element, bool initializeDefault = false)
    {
        ArgumentNullException.ThrowIfNull(element);
        var pragmas = element.GetCustomProperty("pragmas-variables");
        if (initializeDefault && string.IsNullOrEmpty(pragmas))
            pragmas = "AA0021,AA0202";
        return pragmas;
    }
    public static bool HasPragmas(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        return !string.IsNullOrEmpty(element.GetPragmas());
    }
    #region Setters for custom properties
    public static void SetLocalVariable(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.AddCustomProperty("local-variable", "true");
    }
    public static void SetGlobalVariable(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.AddCustomProperty("global-variable", "true");
    }
    public static void SetObjectProperty(this IDocumentedElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.AddCustomProperty("object-property", "true");
    }
    public static void SetPragmas(this IDocumentedElement element, Collection<string> pragmas)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(pragmas);
        element.AddCustomProperty("pragmas", string.Join(",", pragmas));
    }
    public static void SetPragmasVariables(this IDocumentedElement element, Collection<string> pragmas)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(pragmas);
        element.AddCustomProperty("pragmas-variables", string.Join(",", pragmas));
    }
    public static void SetSource(this IDocumentedElement element, string source)
    {
        ArgumentNullException.ThrowIfNull(element);
        element.AddCustomProperty("source", source);
    }
    public static void SetSourceFromProperty(this IDocumentedElement element, CodeProperty property)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentNullException.ThrowIfNull(property);
        switch (property.Type.CollectionKind)
        {
            case CodeTypeCollectionKind.None:
                element.SetSource("from property");
                break;
            case CodeTypeCollectionKind.Complex:
                element.SetSource("from property");
                element.AddCustomProperty("source-type", "List");
                element.AddCustomProperty("return-variable-name", "CodeunitList");
                break;
        }
    }
    #endregion
    #region Type-helpers
    public static bool IsParameter(this IDocumentedElement parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return String.IsNullOrEmpty(parameter.GetCustomProperty("parameter-type")) && !parameter.IsVariable(); // if its a regular Parameter there is no custom property
    }
    public static bool IsVariable(this IDocumentedElement parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.IsGlobalVariable() || parameter.IsLocalVariable();
    }
    public static bool IsGlobalVariable(this IDocumentedElement parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.GetCustomProperty("global-variable") == "true";
    }
    public static bool IsLocalVariable(this IDocumentedElement parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        return parameter.GetCustomProperty("local-variable") == "true";
    }
    public static bool IsObjectProperty(this IDocumentedElement property)
    {
        ArgumentNullException.ThrowIfNull(property);
        return property.GetCustomProperty("object-property") == "true";
    }
    #endregion
}