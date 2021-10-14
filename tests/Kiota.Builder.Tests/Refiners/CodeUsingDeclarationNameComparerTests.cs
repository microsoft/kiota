using System.Linq;
using Xunit;

namespace Kiota.Builder.Refiners.Tests {
    public class CodeUsingDeclarationNameComparerTests {
        private readonly CodeUsingDeclarationNameComparer comparer = new ();
        [Fact]
        public void Defensive() {
            Assert.True(comparer.Equals(null, null));
            Assert.False(comparer.Equals(new (), null));
            Assert.False(comparer.Equals(null, new ()));
            Assert.True(comparer.Equals(new (), new ()));
            Assert.Equal(0, comparer.GetHashCode(null));
            Assert.Equal(0, comparer.GetHashCode(new ()));
        }
        [Fact]
        public void SameImportsReturnSameHashCode() {
            var root = CodeNamespace.InitRootNamespace();
            var graphNS = root.AddNamespace("Graph");
            var modelsNS = root.AddNamespace($"{graphNS.Name}.Models");
            var rbNS = root.AddNamespace($"{graphNS.Name}.Me");
            var modelClass = modelsNS.AddClass(new CodeClass { Name = "Model"}).First();
            var rbClass = rbNS.AddClass(new CodeClass { Name = "UserRequestBuilder"}).First();
            var using1 = new CodeUsing { 
                Name = modelsNS.Name,
                Declaration = new CodeType {
                    Name = modelClass.Name,
                    TypeDefinition = modelClass,
                }
            };
            var using2 = new CodeUsing { 
                Name = modelsNS.Name.ToUpperInvariant(),
                Declaration = new CodeType {
                    Name = modelClass.Name.ToLowerInvariant(),
                    TypeDefinition = modelClass,
                }
            };
            rbClass.AddUsing(using1);
            rbClass.AddUsing(using2);
            Assert.Equal(comparer.GetHashCode(using1), comparer.GetHashCode(using2));
        }
    }
}
