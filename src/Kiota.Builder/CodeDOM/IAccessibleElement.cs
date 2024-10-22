namespace Kiota.Builder.CodeDOM;

/// <summary>
/// Defines a contract for elements that can have configurable access to them using <see cref="AccessModifier"/> 
/// </summary>
public interface IAccessibleElement
{
    AccessModifier Access
    {
        get; set;
    }
}
