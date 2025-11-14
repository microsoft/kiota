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
                DescriptionTemplate = "some description",
            },
            ReturnType = new CodeType(),
            IndexParameter = new() { Name = "param", Type = new CodeType(), }
        };
    }
}
