using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Binding;

public class CollectionBindingTests {
    [Fact]
    public void Should_Resolve_Option_Value_On_Get_Bound_Value() {
        var option = new Option<string>("--name");
        var binding = new CollectionBinding(option);
        var cmd = new RootCommand();
        cmd.AddOption(option);
        var builder = new CommandLineBuilder(cmd);
        object? result = null;
        cmd.SetHandler<object[]>(parameters => result = parameters[0], binding);

        builder.Build().Invoke("--name Test");

        Assert.NotNull(result);
        Assert.Equal("Test", result);
    }
}
