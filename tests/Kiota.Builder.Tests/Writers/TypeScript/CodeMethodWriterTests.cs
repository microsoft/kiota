using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests {
    public class CodeMethodWriterTests : IDisposable {
        private const string defaultPath = "./";
        private const string defaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeMethod method;
        private const string methodName = "methodName";
        private const string returnTypeName = "Somecustomtype";
        public CodeMethodWriterTests()
        {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, defaultPath, defaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            var parentClass = new CodeClass(root) {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            method = new CodeMethod(parentClass) {
                Name = methodName,
            };
            method.ReturnType = new CodeType(method) {
                Name = returnTypeName
            };
            parentClass.AddMethod(method);
        }
        public void Dispose()
        {
            tw?.Dispose();
        }
        [Fact]
        public void ThrowsIfParentIsNotClass() {
            method.Parent = CodeNamespace.InitRootNamespace();
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void ThrowsIfReturnTypeIsMissing() {
            method.ReturnType = null;
            Assert.Throws<InvalidOperationException>(() => writer.Write(method));
        }
        [Fact]
        public void WritesReturnType() {
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains(methodName, result);
            Assert.Contains(returnTypeName, result);
            Assert.Contains("Promise<", result);// async default
            Assert.Contains("| undefined", result);// nullable default
        }
        [Fact]
        public void DoesNotAddUndefinedOnNonNullableReturnType(){
            method.ReturnType.IsNullable = false;
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain("| undefined", result);
        }
        [Fact]
        public void DoesNotAddAsyncInformationOnSyncMethods() {
            method.IsAsync = false;
            writer.Write(method);
            var result = tw.ToString();
            Assert.DoesNotContain("Promise<", result);
            Assert.DoesNotContain("async", result);
        }
        //TODO: we might want to move those into the convention service tests
        [Fact]
        public void WritesPublicMethodByDefault() {
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("public ", result);// public default
        }
        [Fact]
        public void WritesPrivateMethod() {
            method.Access = AccessModifier.Private;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("private ", result);
        }
        [Fact]
        public void WritesProtectedMethod() {
            method.Access = AccessModifier.Protected;
            writer.Write(method);
            var result = tw.ToString();
            Assert.Contains("protected ", result);
        }
    }
    
}
