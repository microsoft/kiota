using System;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CSharpWriterTests {
        [Fact]
        public void Instanciates() {
            var writer =  new CSharpWriter("./", "graph");
            Assert.NotNull(writer);
            Assert.NotNull(writer.PathSegmenter);
            Assert.Throws<ArgumentNullException>(() => new CSharpWriter(null, "graph"));
            Assert.Throws<ArgumentNullException>(() => new CSharpWriter("./", null));
        }
    }
    
}
