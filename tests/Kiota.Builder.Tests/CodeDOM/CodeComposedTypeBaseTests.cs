using System.Linq;
using Kiota.Builder.CodeDOM;
using Xunit;

namespace Kiota.Builder.Tests.CodeDOM
{
    public class CodeComposedTypeBaseTests
    {
        private readonly CodeType currentType;
        private const string TypeName = "SomeType";
        public CodeComposedTypeBaseTests()
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
}
