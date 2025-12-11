namespace Kiota.Builder.CodeDOM;

public interface IDeprecableElement
{
    /// <summary>
    /// Provides detailed information about the deprecation of the element.
    /// </summary>
    DeprecationInformation? Deprecation
    {
        get; set;
    }
}
