using Kiota.Builder.CodeDOM;
using Xunit;

namespace Kiota.Builder.Tests.CodeDOM;
public class CodeIndexerTests
{
    [Fact]
    public void IndexerInits()
    {
        _ = new CodeIndexer
        {
            Name = "idx",
            Documentation = new()
            {
                Description = "some description",
            },
            IndexType = new CodeType(),
            ReturnType = new CodeType()
        };
    }
}
