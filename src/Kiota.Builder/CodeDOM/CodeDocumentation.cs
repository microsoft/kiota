
using System;

namespace Kiota.Builder.CodeDOM;

/// <summary>
/// The documentation information of a code element.
/// </summary>
public class CodeDocumentation : ICloneable
{
    /// <summary>
    /// The description of the current element.
    /// </summary>
    public string Description
    {
        get; set;
    }
    /// <summary>
    /// The external documentation link for this method.
    /// </summary>
    public Uri DocumentationLink
    {
        get; set;
    }
    ///<summary>
    ///The label for the external documentation link.
    ///</summary>
    public string DocumentationLabel
    {
        get; set;
    }

    /// <inheritdoc/>
    public object Clone()
    {
        return new CodeDocumentation {
            Description = Description?.Clone() as string,
            DocumentationLink = DocumentationLink == null ? null : new (DocumentationLink.ToString()),
            DocumentationLabel = DocumentationLabel?.Clone() as string,
        };
    }
    public bool DescriptionAvailable { get => !string.IsNullOrEmpty(Description); }
    public bool ExternalDocumentationAvailable { get => DocumentationLink != null && !string.IsNullOrEmpty(DocumentationLabel); }
}
