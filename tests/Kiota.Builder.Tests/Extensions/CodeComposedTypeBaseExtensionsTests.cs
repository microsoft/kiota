using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Linq;
using Kiota.Builder.CodeDOM;
using Kiota.Builder.Extensions;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;
public class CodeComposedTypeBaseExtensionsTests
{
    private readonly CodeType currentType;
    private const string TypeName = "SomeType";
    public CodeComposedTypeBaseExtensionsTests()
    {
        currentType = new()
        {
            Name = TypeName
        };
        var root = CodeNamespace.InitRootNamespace();
        var parentClass = root.AddClass(new CodeClass
        {
            Name = "ParentClass"
        }).First();
        currentType.Parent = parentClass;
    }

    [Fact]
    public void IsComposedOfPrimitives_ShouldBeTrue_WhenComposedOfPrimitives()
    {
        var composedType = new CodeUnionType
        {
            Name = "test",
            Parent = currentType
        };
        composedType.AddType(new CodeType { Name = "string", IsExternal = true });
        composedType.AddType(new CodeType { Name = "integer", IsExternal = true });
        Assert.True(composedType.IsComposedOfPrimitives());
    }

    [Fact]
    public void IsComposedOfPrimitives_ShouldBeFalse_WhenNotComposedOfPrimitives()
    {
        var composedType = new CodeUnionType
        {
            Name = "test",
            Parent = currentType
        };
        composedType.AddType(new CodeType { Name = "string", IsExternal = true });
        CodeClass td = new CodeClass
        {
            Name = "SomeClass"
        };
        composedType.AddType(new CodeType { Name = "SomeCustomObject", IsExternal = false, TypeDefinition = td });
        Assert.False(composedType.IsComposedOfPrimitives());
    }
}
