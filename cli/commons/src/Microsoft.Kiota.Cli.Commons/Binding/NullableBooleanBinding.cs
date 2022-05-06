using System.CommandLine;
using System.CommandLine.Binding;

namespace Microsoft.Kiota.Cli.Commons.Binding;

public class NullableBooleanBinding : BinderBase<bool?>
{
    private readonly Option<bool?> option;

    public NullableBooleanBinding(Option<bool?> option)
    {
        this.option = option;
    }

    protected override bool? GetBoundValue(BindingContext bindingContext)
    {
        return bindingContext.ParseResult.GetValueForOption(this.option);
    }
}
