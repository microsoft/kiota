using System;
using System.Collections.Generic;
using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeElementComparerTests {
        [Fact]
        public void OrdersWithMethodWithinClass() {
            var root = CodeNamespace.InitRootNamespace();
            var comparer = new CodeElementOrderComparer();
            var codeClass = new CodeClass {
                Name = "Class"
            };
            root.AddClass(codeClass);
            var method = new CodeMethod {
                Name = "Method"
            };
            codeClass.AddMethod(method);
            method.AddParameter(new CodeParameter {
                Name = "param"
            });
            var dataSet = new List<Tuple<CodeElement, CodeElement, int>> {
                new(null, null, 0),
                new(null, new CodeClass(), -1),
                new(new CodeClass(), null, 1),
                new(new CodeUsing(), new CodeProperty(), -100),
                new(new CodeIndexer(), new CodeProperty(), 100),
                new(method, new CodeProperty(), 111),
                new(method, codeClass, -89)
                
            };
            foreach(var dataEntry in dataSet) {
                Assert.Equal(dataEntry.Item3, comparer.Compare(dataEntry.Item1, dataEntry.Item2));
            }
        }
        [Fact]
        public void OrdersWithMethodsOutsideOfClass() {
            var root = CodeNamespace.InitRootNamespace();
            var comparer = new CodeElementOrderComparerWithExternalMethods();
            var codeClass = new CodeClass {
                Name = "Class"
            };
            root.AddClass(codeClass);
            var method = new CodeMethod {
                Name = "Method"
            };
            method.AddParameter(new CodeParameter {
                Name = "param"
            });
            codeClass.AddMethod(method);
            var dataSet = new List<Tuple<CodeElement, CodeElement, int>> {
                new(null, null, 0),
                new(null, new CodeClass(), -1),
                new(new CodeClass(), null, 1),
                new(new CodeUsing(), new CodeProperty(), -100),
                new(new CodeIndexer(), new CodeProperty(), 100),
                new(method, new CodeProperty(), 111),
                new(method, codeClass, 111)
                
            };
            foreach(var dataEntry in dataSet) {
                Assert.Equal(dataEntry.Item3, comparer.Compare(dataEntry.Item1, dataEntry.Item2));
            }
        }
    }
}
