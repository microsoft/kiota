using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// The documentation information of a code element.
/// </summary>
public class CodeDocumentation : ICloneable
{
    /// <summary>
    /// Instantiates a new instance of the <see cref="CodeDocumentation"/> class.
    /// </summary>
    /// <param name="typeReferences">The references used by the description</param>
    public CodeDocumentation(Dictionary<string, CodeTypeBase>? typeReferences = null)
    {
        if (typeReferences is not null)
            TypeReferences = new(typeReferences, StringComparer.OrdinalIgnoreCase);
    }
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
    public string GetDescription(Func<CodeTypeBase, string> typeReferenceResolver, string? typeReferencePrefix = null, string? typeReferenceSuffix = null, Func<string, string>? normalizationFunc = null)
    {
        return GetDescriptionInternal(DescriptionTemplate, typeReferenceResolver, TypeReferences, typeReferencePrefix, typeReferenceSuffix, normalizationFunc);
    }

    internal static string GetDescriptionInternal(string descriptionTemplate, Func<CodeTypeBase, string> typeReferenceResolver, IDictionary<string, CodeTypeBase>? typeReferences = null, string? typeReferencePrefix = null, string? typeReferenceSuffix = null, Func<string, string>? normalizationFunc = null)
    {
        ArgumentNullException.ThrowIfNull(typeReferenceResolver);
        if (string.IsNullOrEmpty(descriptionTemplate))
        {
            return string.Empty;
        }

        var description = normalizationFunc is null ? descriptionTemplate : normalizationFunc(descriptionTemplate);

        if (typeReferences is not null)
        {
            foreach (var kvp in typeReferences)
            {
                var key = kvp.Key;
                var value = kvp.Value;

                string resolvedValue;

                switch (value)
                {
                    case CodeComposedTypeBase codeComposedTypeBase:
                        var composedTypeValues = new List<string>();
                        foreach (var type in codeComposedTypeBase.Types)
                        {
                            composedTypeValues.Add($"{typeReferencePrefix}{typeReferenceResolver(type)}{typeReferenceSuffix}");
                        }
                        var composedTypeResult = string.Join(", ", composedTypeValues);
                        resolvedValue = string.IsNullOrEmpty(composedTypeResult) ? typeReferenceResolver(value) : composedTypeResult;
                        break;

                    default:
                        resolvedValue = $"{typeReferencePrefix}{typeReferenceResolver(value)}{typeReferenceSuffix}";
                        break;
                }

                if (!string.IsNullOrEmpty(resolvedValue))
                {
                    description = description.Replace($"{{{key}}}", resolvedValue, StringComparison.OrdinalIgnoreCase);
                }
            }
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
