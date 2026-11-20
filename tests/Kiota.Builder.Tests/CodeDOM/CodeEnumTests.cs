using System.Linq;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodeEnumTests
{
    [Fact]
    public void EnumInits()
    {
        var root = CodeNamespace.InitRootNamespace();
        var codeEnum = root.AddEnum(new CodeEnum
        {
            Name = "Enum",
            Documentation = new()
            {
                DescriptionTemplate = "some description",
            },
            Flags = true,
        }).First();
        codeEnum.AddOption(new CodeEnumOption { Name = "option1" });
    }

    [Fact]
    public void PreservesExplicitEmptySerializationName()
    {
        var option = new CodeEnumOption
        {
            Name = "Empty",
            SerializationName = string.Empty,
        };

        Assert.True(option.IsNameEscaped);
        Assert.Empty(option.WireName);
        Assert.Equal("Empty", option.SymbolName);
    }
}
