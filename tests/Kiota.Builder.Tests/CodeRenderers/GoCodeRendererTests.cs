using Kiota.Builder.CodeRenderers;

using Xunit;

namespace Kiota.Builder.Tests.CodeRenderers;

public sealed class GoCodeRendererTests
{
    [Fact]
    public void InsertsASingleBlankLineBetweenTopLevelDeclarations()
    {
        var source =
            "package models\n" +
            "type Example struct {\n" +
            "\tid *string\n" +
            "}\n" +
            "// NewExample instantiates a new Example.\n" +
            "func NewExample() *Example {\n" +
            "\treturn &Example{}\n" +
            "}\n";

        var expected =
            "package models\n" +
            "\n" +
            "type Example struct {\n" +
            "\tid *string\n" +
            "}\n" +
            "\n" +
            "// NewExample instantiates a new Example.\n" +
            "func NewExample() *Example {\n" +
            "\treturn &Example{}\n" +
            "}\n";

        Assert.Equal(expected, GoCodeRenderer.FormatGoSource(source));
    }

    [Fact]
    public void CollapsesMultipleBlankLinesAndStripsLeadingAndTrailingBlankLines()
    {
        var source =
            "\n\n" +
            "package models\n" +
            "\n" +
            "\n" +
            "type Example struct {\n" +
            "\tid *string\n" +
            "}\n" +
            "\n\n\n";

        var expected =
            "package models\n" +
            "\n" +
            "type Example struct {\n" +
            "\tid *string\n" +
            "}\n";

        Assert.Equal(expected, GoCodeRenderer.FormatGoSource(source));
    }

    [Fact]
    public void TrimsTrailingWhitespace()
    {
        var source =
            "package models\n" +
            "\n" +
            "// Deprecated: \n" +
            "func Example() {\t\n" +
            "}\n";

        var expected =
            "package models\n" +
            "\n" +
            "// Deprecated:\n" +
            "func Example() {\n" +
            "}\n";

        Assert.Equal(expected, GoCodeRenderer.FormatGoSource(source));
    }

    [Fact]
    public void AlignsConsecutiveStructFieldsIntoColumns()
    {
        var source =
            "package models\n" +
            "\n" +
            "type QueryParameters struct {\n" +
            "\tActive *bool \"uriparametername:\\\"active\\\"\"\n" +
            "\tKeyword *string \"uriparametername:\\\"keyword\\\"\"\n" +
            "}\n";

        var expected =
            "package models\n" +
            "\n" +
            "type QueryParameters struct {\n" +
            "\tActive  *bool   \"uriparametername:\\\"active\\\"\"\n" +
            "\tKeyword *string \"uriparametername:\\\"keyword\\\"\"\n" +
            "}\n";

        Assert.Equal(expected, GoCodeRenderer.FormatGoSource(source));
    }

    [Fact]
    public void DoesNotPadTheTrailingCellOfAField()
    {
        // EndDateTime has no tag so its type is the last cell on the row and must not be padded.
        var source =
            "package models\n" +
            "\n" +
            "type QueryParameters struct {\n" +
            "\tEndDateTime *string\n" +
            "\tStartDateTime *string \"uriparametername:\\\"startDateTime\\\"\"\n" +
            "}\n";

        var expected =
            "package models\n" +
            "\n" +
            "type QueryParameters struct {\n" +
            "\tEndDateTime   *string\n" +
            "\tStartDateTime *string \"uriparametername:\\\"startDateTime\\\"\"\n" +
            "}\n";

        Assert.Equal(expected, GoCodeRenderer.FormatGoSource(source));
    }

    [Fact]
    public void AlignsConstBlocksButNotImportBlocks()
    {
        var source =
            "package models\n" +
            "\n" +
            "import (\n" +
            "\talias \"github.com/example/long-module-path\"\n" +
            "\tb \"github.com/example/b\"\n" +
            ")\n" +
            "\n" +
            "const (\n" +
            "\tROCK_MOVETYPE = 1\n" +
            "\tPAPER_MOVETYPE = 2\n" +
            "\tSCISSORS_MOVETYPE = 4\n" +
            ")\n";

        var expected =
            "package models\n" +
            "\n" +
            "import (\n" +
            "\talias \"github.com/example/long-module-path\"\n" +
            "\tb \"github.com/example/b\"\n" +
            ")\n" +
            "\n" +
            "const (\n" +
            "\tROCK_MOVETYPE     = 1\n" +
            "\tPAPER_MOVETYPE    = 2\n" +
            "\tSCISSORS_MOVETYPE = 4\n" +
            ")\n";

        Assert.Equal(expected, GoCodeRenderer.FormatGoSource(source));
    }

    [Fact]
    public void DoesNotAlignFieldsSeparatedByComments()
    {
        // Fields documented by their own comment form separate alignment groups, like gofmt.
        var source =
            "package models\n" +
            "\n" +
            "type Model struct {\n" +
            "\t// The id property\n" +
            "\tid *string\n" +
            "\t// The displayName property\n" +
            "\tdisplayName *string\n" +
            "}\n";

        Assert.Equal(source, GoCodeRenderer.FormatGoSource(source));
    }
}
