using Kiota.Builder.CodeDOM;
using Kiota.Builder.Refiners;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;

public class CodeUsingComparerTests
{
    [Fact]
    public void ComparesWithDeclaration()
    {
        var root = CodeNamespace.InitRootNamespace();
        var cUsing = new CodeUsing
        {
            Name = "using1",
        };
        cUsing.Declaration = new CodeType
        {
            Name = "type1"
        };

        var cUsing2 = new CodeUsing
        {
            Name = "using2",
        };
        cUsing2.Declaration = new CodeType
        {
            Name = "type2"
        };
        var comparer = new CodeUsingComparer(true);
        Assert.False(comparer.Equals(cUsing, cUsing2));
        Assert.NotEqual(0, comparer.GetHashCode(cUsing));
    }
}
