using System;
using Xunit;

namespace Kiota.Builder.Writers.Go.Tests {
    public class GoWriterTests {
        [Fact]
        public void Instanciates() {
            var writer =  new GoWriter("./", "graph", false);
            Assert.NotNull(writer);
            Assert.NotNull(writer.PathSegmenter);
            Assert.Throws<ArgumentNullException>(() => new GoWriter(null, "graph", false));
            Assert.Throws<ArgumentNullException>(() => new GoWriter("./", null, false));
        }
    }
    
}
