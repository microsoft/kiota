using System.Linq;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodeUnionTypeTests
{
    [Fact]
    public void ClonesProperly()
    {
        var root = CodeNamespace.InitRootNamespace();
        var type = new CodeUnionType
        {
            Name = "type1",
        };
        type.AddType(new CodeType
        {
            Name = "subtype"
        });
        var clone = type.Clone() as CodeUnionType;
        Assert.NotNull(clone);
        Assert.Single(clone.AllTypes);
        Assert.Single(clone.Types);
        Assert.Equal(type.AllTypes.First().Name, clone.AllTypes.First().Name);
    }
}
