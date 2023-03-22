using System;
using System.Collections.Generic;

using Kiota.Builder.CodeDOM;

using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;
public class CodeElementComparerPythonTests
{
    [Fact]
    public void OrdersWithMethodWithinClass()
    {
        var root = CodeNamespace.InitRootNamespace();
        var comparer = new CodeElementOrderComparerPython();
        var codeClass = new CodeClass
        {
            Name = "Class"
        };
        root.AddClass(codeClass);
        var method = new CodeMethod
        {
            Name = "Method",
            ReturnType = new CodeType
            {
                Name = "string"
            }
        };
        codeClass.AddMethod(method);
        method.AddParameter(new CodeParameter
        {
            Name = "param",
            Type = new CodeType
            {
                Name = "string"
            }
        });
        var dataSet = new List<Tuple<CodeElement, CodeElement, int>> {
            new(null, null, 0),
            new(null, new CodeClass(), -1),
            new(new CodeClass(), null, 1),
            new(new CodeUsing(), new CodeProperty() {
                Name = "prop",
                Type = new CodeType {
                    Name = "string"
                }
            }, -1100),
            new(new CodeIndexer() {
                ReturnType = new CodeType {
                    Name = "string"
                },
                IndexType = new CodeType {
                    Name = "string"
                }
            }, new CodeProperty() {
                Name = "prop",
                Type = new CodeType {
                    Name = "string"
                }
            }, -1100),
            new(method, new CodeProperty() {
                Name = "prop",
                Type = new CodeType {
                    Name = "string"
                }
            }, -899),
            new(method, codeClass, -699),
            new(new CodeMethod() {
                Kind = CodeMethodKind.Constructor,
                ReturnType = new CodeType
                {
                    Name = "null",
                }
            }, method, -301),
            new(new CodeMethod() {
                Kind = CodeMethodKind.ClientConstructor,
                ReturnType = new CodeType
                {
                    Name = "null",
                }
            }, method, -301),

        };
        foreach (var dataEntry in dataSet)
        {
            Assert.Equal(dataEntry.Item3, comparer.Compare(dataEntry.Item1, dataEntry.Item2));
        }
    }
}
