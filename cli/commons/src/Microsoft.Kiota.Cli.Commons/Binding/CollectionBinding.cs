using System.CommandLine;
using System.CommandLine.Binding;

namespace Microsoft.Kiota.Cli.Commons.Binding;

/// <summary>
/// Binds a collection of <see cref="IValueDescriptor"/> types and passes the bound values into the handler action as an array.
/// </summary>
public class CollectionBinding : BinderBase<object[]>
{
    private readonly IValueDescriptor[] _symbols;
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="symbols">The symbols to resolve. Can be <see cref="Option"/>, <see cref="Argument"/> or any other <see cref="BinderBase{T}"/></param>
    public CollectionBinding(params IValueDescriptor[] symbols)
    {
        this._symbols = symbols;
    }

    /// <inheritdoc/>
    protected override object[] GetBoundValue(BindingContext bindingContext)
    {
        var result = new object[_symbols.Length];
        for (int i = 0; i < _symbols.Length; i++)
        {
            object? value = GetValueForHandlerParameter(_symbols, i, bindingContext);
            if (value != null)
            {
                result[i] = value;
            }
        }
        return result;
    }

    private static object? GetValueForHandlerParameter(
        IValueDescriptor[] symbols,
        int index,
        BindingContext context)
    {
        if (symbols.Length <= index) {
            throw new ArgumentOutOfRangeException(nameof(index), index, "The index is out of range.");
        }

        if (symbols[index] is IValueDescriptor symbol)
        {
            if (symbol is IValueSource valueSource &&
                valueSource.TryGetValue(symbol, context, out var boundValue))
            {
                return boundValue;
            }
            else
            {
                if (symbol is Argument argument)
                {
                    return context.ParseResult.GetValueForArgument(argument);
                }
                else if (symbol is Option option)
                {
                    return context.ParseResult.GetValueForOption(option);
                }
                else
                {
                    throw new ArgumentOutOfRangeException($"The symbol at index {index} does not correspond to an option or argument.", null as Exception);
                }
            }
        }

        throw new ArgumentException($"The symbol at index {index} is not of type {typeof(IValueDescriptor)}");
    }
}
