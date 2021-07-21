using System;
using Xunit;

namespace Kiota.Builder.Writers.Ruby.Tests {
    public class RubyWriterTests {
        [Fact]
        public void Instanciates() {
            var writer =  new RubyWriter("./", "graph");
            Assert.NotNull(writer);
            Assert.NotNull(writer.PathSegmenter);
            Assert.Throws<ArgumentNullException>(() => new RubyWriter(null, "graph"));
            Assert.Throws<ArgumentNullException>(() => new RubyWriter("./", null));
        }
    }
    
}
