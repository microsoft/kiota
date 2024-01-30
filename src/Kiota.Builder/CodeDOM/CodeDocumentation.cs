using System;
using System.Collections.Concurrent;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// The documentation information of a code element.
/// </summary>
public class CodeDocumentation : ICloneable
{
    /// <summary>
    /// The description of the current element.
    /// </summary>
    public string DescriptionTemplate
    {
        get; set;
    } = string.Empty;
    /// <summary>
    /// The external documentation link for this method.
    /// </summary>
    public Uri? DocumentationLink
    {
        get; set;
    }
    ///<summary>
    ///The label for the external documentation link.
    ///</summary>
    public string DocumentationLabel
    {
        get; set;
    } = string.Empty;

    /// <inheritdoc/>
    public object Clone()
    {
        return new CodeDocumentation
        {
            DescriptionTemplate = DescriptionTemplate,
            DocumentationLink = DocumentationLink == null ? null : new(DocumentationLink.ToString()),
            DocumentationLabel = DocumentationLabel,
            TypeReferences = new(TypeReferences, StringComparer.OrdinalIgnoreCase)
        };
    }
    /// <summary>
    /// References to be resolved when the description is emitted.
    /// Keys MUST match the description template tokens or they will be ignored.
    /// </summary>
    public ConcurrentDictionary<string, CodeTypeBase> TypeReferences { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
    public string GetDescription(Func<CodeTypeBase, string> typeReferenceResolver)
    {
        ArgumentNullException.ThrowIfNull(typeReferenceResolver);
        if (string.IsNullOrEmpty(DescriptionTemplate))
            return string.Empty;
        var description = DescriptionTemplate;
        foreach (var (key, value) in TypeReferences)
        {
            var resolvedValue = typeReferenceResolver(value);
            if (!string.IsNullOrEmpty(resolvedValue))
                description = description.Replace($"{{{key}}}", resolvedValue, StringComparison.OrdinalIgnoreCase);
        }
        return description;
    }
    public bool DescriptionAvailable
    {
        get => !string.IsNullOrEmpty(DescriptionTemplate);
    }
    public bool ExternalDocumentationAvailable
    {
        get => DocumentationLink != null && !string.IsNullOrEmpty(DocumentationLabel);
    }
}
