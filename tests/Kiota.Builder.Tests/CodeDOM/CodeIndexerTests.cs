using Xunit;

namespace Kiota.Builder.tests {
    public class CodeIndexerTests {
        [Fact]
        public void IndexerInits() {
            var root = CodeNamespace.InitRootNamespace();
            var indexer = new CodeIndexer(root) {
                Name = "idx",
                Description = "some description"
            };
            indexer.IndexType = new CodeType(indexer) {};
            indexer.ReturnType = new CodeType(indexer) {};
        }
    }
    
}
