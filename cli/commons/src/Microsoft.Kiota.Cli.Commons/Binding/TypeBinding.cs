using System.CommandLine.Binding;

namespace Microsoft.Kiota.Cli.Commons.Binding;

/// <summary>
/// Binding used to resolve a value based on the <see cref="System.Type"/>
/// </summary>
public class TypeBinding : BinderBase<object>
{
    private readonly Type _valueType;
    
    /// <summary></summary>
    /// <param name="valueType">The type information of the value</param>
    public TypeBinding(Type valueType)
    {
        this._valueType = valueType;
    }

    /// <inheritdoc/>
    protected override object GetBoundValue(BindingContext bindingContext)
    {
        return bindingContext.GetService(_valueType) ?? throw new ArgumentException($"Service not found for type {_valueType}.");
    }
}
