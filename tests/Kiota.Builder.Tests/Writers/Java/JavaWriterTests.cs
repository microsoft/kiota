using System;
using Xunit;

namespace Kiota.Builder.Writers.Java.Tests {
    public class JavaWriterTests {
        [Fact]
        public void Instanciates() {
            var writer =  new JavaWriter("./", "graph", false);
            Assert.NotNull(writer);
            Assert.NotNull(writer.PathSegmenter);
            Assert.Throws<ArgumentNullException>(() => new JavaWriter(null, "graph", false));
            Assert.Throws<ArgumentNullException>(() => new JavaWriter("./", null, false));
        }
    }
    
}
