using System;
using Xunit;

namespace Kiota.Builder.Writers.TypeScript.Tests {
    public class TypeScriptWriterTests {
        [Fact]
        public void Instanciates() {
            var writer =  new TypeScriptWriter("./", "graph", false);
            Assert.NotNull(writer);
            Assert.NotNull(writer.PathSegmenter);
            Assert.Throws<ArgumentNullException>(() => new TypeScriptWriter(null, "graph", false));
            Assert.Throws<ArgumentNullException>(() => new TypeScriptWriter("./", null, false));
        }
    }
    
}
