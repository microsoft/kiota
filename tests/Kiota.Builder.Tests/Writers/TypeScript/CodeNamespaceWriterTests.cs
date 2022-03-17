using System;
using System.IO;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests
{
    public class CodeNameSpaceWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly LanguageWriter writer;

        public CodeNameSpaceWriterTests()
        {
            writer = LanguageWriter.GetLanguageWriter(GenerationLanguage.TypeScript, DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
        }
        
        public void Dispose()
        {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void WritesOnlyModelClasses()
        {
            var root = CodeNamespace.InitRootNamespace();
            var requestbuilder = new CodeClass
            {
                Kind = CodeClassKind.RequestBuilder,
                Name = "TestRequestBuilder",
            };
            var model = new CodeClass
            {
                Kind = CodeClassKind.Model,
                Name = "TestModel", // The tests should verify if the printed file names start with lower case.
            };
            root.AddClass(requestbuilder);
            root.AddClass(model);
            writer.Write(root);
            var result = tw.ToString();
            Console.WriteLine(result);
            Assert.Contains("export * from './testModel'", result);
        }

        [Fact]
        public void SortModelClassesBasedonInheritance()
        {
            var root = CodeNamespace.InitRootNamespace();
            var modelA = new CodeClass
            {
                Kind = CodeClassKind.Model,
                Name = "ModelA",
            };
            var modelB = new CodeClass
            {
                Kind = CodeClassKind.Model,
                Name = "ModelB",
                Parent = modelA
            };

            var declarationB = modelB.StartBlock;

            declarationB.Inherits = new CodeType()
            {
                TypeDefinition = new CodeType()
                {
                    Parent = modelA
                }
            };

            var modelC = new CodeClass
            {
                Kind = CodeClassKind.Model,
                Name = "ModelC",
                Parent = modelB
            };

            var declarationC = modelC.StartBlock;

            declarationC.Inherits = new CodeType()
            {
                TypeDefinition = new CodeType()
                {
                    Parent= modelB
                }
            };

            root.AddClass(modelA);
            root.AddClass(modelB);
            root.AddClass(modelC);

            writer.Write(root);
            var result = tw.ToString();
            Console.WriteLine(result);
            Assert.Contains("export * from './modelA'\r\nexport * from './modelB'\r\nexport * from './modelC'\r\n'", result);
        }
    }
}
