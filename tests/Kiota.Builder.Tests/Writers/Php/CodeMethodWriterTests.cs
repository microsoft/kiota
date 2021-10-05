using System;
using System.IO;
using Kiota.Builder.Writers;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Php
{
    public class CodeMethodWriterTests: IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;
        private readonly CodeMethod method;
        private readonly CodeClass parentClass;
        private const string MethodName = "methodName";
        private const string ReturnTypeName = "Somecustomtype";
        private const string MethodDescription = "some description";
        private const string ParamDescription = "some parameter description";
        private const string ParamName = "paramName";

        public CodeMethodWriterTests()
        {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.PHP, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
            var root = CodeNamespace.InitRootNamespace();
            parentClass = new CodeClass() {
                Name = "parentClass"
            };
            root.AddClass(parentClass);
            method = new CodeMethod() {
                Name = MethodName,
            };
            method.ReturnType = new CodeType() {
                Name = ReturnTypeName
            };
            parentClass.AddMethod(method);
        }
        [Fact]
        public void Test()
        {
            tw.Write(parentClass);
        }

        public void Dispose()
        {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
