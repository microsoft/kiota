using System;
using Xunit;

namespace Kiota.Builder.Writers.CSharp.Tests {
    public class CSharpWriterTests {
        [Fact]
        public void Instanciates() {
            var writer =  new CSharpWriter("./", "graph", false);
            Assert.NotNull(writer);
            Assert.NotNull(writer.PathSegmenter);
            Assert.Throws<ArgumentNullException>(() => new CSharpWriter(null, "graph", false));
            Assert.Throws<ArgumentNullException>(() => new CSharpWriter("./", null, false));
        }
    }
    
}
