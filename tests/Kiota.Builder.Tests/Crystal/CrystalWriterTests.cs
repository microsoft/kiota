using System;
using System.IO;
using Kiota.Builder.Writers.Crystal;
using Xunit;

namespace Kiota.Builder.Tests.Writers.Crystal
{
    public sealed class CrystalWriterTests : IDisposable
    {
        private const string DefaultPath = "./";
        private const string DefaultName = "name";
        private readonly StringWriter tw;
        private readonly CrystalWriter writer;

        public CrystalWriterTests()
        {
            writer = new CrystalWriter(DefaultPath, DefaultName);
            tw = new StringWriter();
            writer.SetTextWriter(tw);
        }

        public void Dispose()
        {
            tw?.Dispose();
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void WritesNamespace()
        {
            writer.WriteLine("module TestNamespace");
            writer.WriteLine("end");
            var result = tw.ToString();
            Assert.Contains("module TestNamespace", result);
            Assert.Contains("end", result);
        }

        [Fact]
        public void WritesClass()
        {
            writer.WriteLine("class TestClass");
            writer.WriteLine("end");
            var result = tw.ToString();
            Assert.Contains("class TestClass", result);
            Assert.Contains("end", result);
        }

        [Fact]
        public void WritesMethod()
        {
            writer.WriteLine("def test_method");
            writer.WriteLine("end");
            var result = tw.ToString();
            Assert.Contains("def test_method", result);
            Assert.Contains("end", result);
        }

        [Fact]
        public void WritesProperty()
        {
            writer.WriteLine("property test_property : String");
            var result = tw.ToString();
            Assert.Contains("property test_property : String", result);
        }

        [Fact]
        public void WritesRequire()
        {
            writer.WriteLine("require \"test\"");
            var result = tw.ToString();
            Assert.Contains("require \"test\"", result);
        }
    }
}

