using System;
using System.Linq;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeMethodTests {
        [Fact]
        public void Defensive() {
            var method = new CodeMethod {
                Name = "class",
            };
            Assert.False(method.IsOfKind((CodeMethodKind[])null));
            Assert.False(method.IsOfKind(Array.Empty<CodeMethodKind>()));
        }
        [Fact]
        public void IsOfKind() {
            var method = new CodeMethod {
                Name = "class",
            };
            Assert.False(method.IsOfKind(CodeMethodKind.Constructor));
            method.Kind = CodeMethodKind.Deserializer;
            Assert.True(method.IsOfKind(CodeMethodKind.Deserializer));
            Assert.True(method.IsOfKind(CodeMethodKind.Deserializer, CodeMethodKind.Getter));
            Assert.False(method.IsOfKind(CodeMethodKind.Getter));
        }
        [Fact]
        public void AddsParameter() {
            var method = new CodeMethod {
                Name = "method1"
            };
            Assert.Throws<ArgumentNullException>(() => {
                method.AddParameter((CodeParameter)null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                method.AddParameter(null);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                method.AddParameter(Array.Empty<CodeParameter>());
            });
        }
        [Fact]
        public void ClonesParameters() {
            var method = new CodeMethod {
                Name = "method1"
            };
            method.AddParameter(new CodeParameter {
                Name = "param1"
            });
            var clone = method.Clone() as CodeMethod;
            Assert.Equal(method.Name, clone.Name);
            Assert.Single(method.Parameters);
            Assert.Equal(method.Parameters.First().Name, clone.Parameters.First().Name);
        }
        [Fact]
        public void ParametersExtensionsReturnsValue() {
            var method = new CodeMethod {
                Name = "method1"
            };
            method.AddParameter(new CodeParameter {
                Name = "param1",
                Kind = CodeParameterKind.Custom,
            });
            Assert.NotNull(method.Parameters.OfKind(CodeParameterKind.Custom));
            Assert.Null(method.Parameters.OfKind(CodeParameterKind.RequestBody));
        }
    }
}
