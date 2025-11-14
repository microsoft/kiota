using System;

namespace Kiota.Builder.CodeDOM;

public class CodeIndexer : CodeTerminal, IDocumentedElement, IDeprecableElement, ICloneable
{
#nullable disable // exposing property is required
    private CodeTypeBase returnType;
#nullable enable
    public required CodeTypeBase ReturnType
    {
        get => returnType; set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            returnType = value;
        }
    }
#nullable disable // exposing property is required
    private CodeParameter indexParameter;
#nullable enable
    /// <summary>
    /// The parameter to use for the indexer.
    /// </summary>
    public required CodeParameter IndexParameter
    {
        get => indexParameter; set
        {
            ArgumentNullException.ThrowIfNull(value);
            EnsureElementsAreChildren(value);
            indexParameter = value;
        }
    }
    public CodeDocumentation Documentation { get; set; } = new();
    /// <summary>
    /// The Path segment to use for the method name when using back-compatible methods.
    /// </summary>
    public string PathSegment { get; set; } = string.Empty;
    public DeprecationInformation? Deprecation
    {
        get; set;
    }
    //TODO remove property for v2
    public bool IsLegacyIndexer
    {
        get; set;
    }

    public object Clone()
    {
        return new CodeIndexer
        {
            Name = Name,
            Parent = Parent,
            ReturnType = (CodeTypeBase)ReturnType.Clone(),
            Documentation = (CodeDocumentation)Documentation.Clone(),
            PathSegment = PathSegment,
            Deprecation = Deprecation == null ? null : Deprecation with { },
            IndexParameter = (CodeParameter)IndexParameter.Clone(),
            IsLegacyIndexer = IsLegacyIndexer,
        };
    }
}
