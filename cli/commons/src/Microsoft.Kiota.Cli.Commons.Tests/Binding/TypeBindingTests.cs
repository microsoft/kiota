using System;
using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Moq;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Binding;

public class TypeBindingTests {
    [Fact]
    public void Should_Resolve_Service_On_Get_Bound_Value() {
        var binding = new TypeBinding(typeof(string));
        var cmd = new RootCommand();
        var builder = new CommandLineBuilder(cmd);
        builder.AddMiddleware(invocation => {
            invocation.BindingContext.AddService<string>(_ => "Test");
        });
        string? result = null;
        cmd.SetHandler<string>(strType => result = strType, binding);

        builder.Build().Invoke("");

        Assert.NotNull(result);
        Assert.Equal("Test", result);
    }

    [Fact]
    public void Should_Throw_Exception_On_Service_Not_Found() {
        var binding = new TypeBinding(typeof(string));
        var cmd = new RootCommand();
        var builder = new CommandLineBuilder(cmd);
        string? result = null;
        cmd.SetHandler<string>(strType => result = strType, binding);

        Assert.Throws<ArgumentException>(() => builder.Build().Invoke(""));
    }
}
