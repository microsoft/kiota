using Xunit;

namespace Kiota.Builder.Tests {
    public class CodeIndexerTests {
        [Fact]
        public void IndexerInits() {
            var indexer = new CodeIndexer {
                Name = "idx",
                Description = "some description"
            };
            indexer.IndexType = new CodeType {};
            indexer.ReturnType = new CodeType {};
        }
    }
    
}
