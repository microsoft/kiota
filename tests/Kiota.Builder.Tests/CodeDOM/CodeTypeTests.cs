using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeTypeTests {
        [Fact]
        public void ClonesTypeProperly() {
            var type = new CodeType {
                Name = "type1",
                ActionOf = true,
                CollectionKind = CodeTypeBase.CodeTypeCollectionKind.Array,
                IsExternal = true,
                IsNullable = true,
            };
            type.TypeDefinition = new CodeClass {
                Name = "class1"
            };
            var clone = type.Clone() as CodeType;

            Assert.True(clone.ActionOf);
            Assert.True(clone.IsExternal);
            Assert.True(clone.IsNullable);
            Assert.Single(clone.AllTypes);
            Assert.Equal(CodeTypeBase.CodeTypeCollectionKind.Array, clone.CollectionKind);
            Assert.Equal(type.TypeDefinition.Name, clone.TypeDefinition.Name);
        }
        
    }
    
}
