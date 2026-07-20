using Kiota.Builder.Writers.Go;

using Xunit;

namespace Kiota.Builder.Tests.Writers.Go;

public sealed class GoFieldFormatterTests
{
    [Fact]
    public void AlignsConsecutiveCommentLessFieldsIntoColumns()
    {
        // typical query parameter struct body: no doc comments, so gofmt pads names and types
        string[] body =
        [
            "\tActive *bool \"uriparametername:\\\"active\\\"\"",
            "\tKeyword *string \"uriparametername:\\\"keyword\\\"\"",
        ];

        var aligned = GoFieldFormatter.AlignFieldBlock(body);

        Assert.Equal(
        [
            "\tActive  *bool   \"uriparametername:\\\"active\\\"\"",
            "\tKeyword *string \"uriparametername:\\\"keyword\\\"\"",
        ], aligned);
    }

    [Fact]
    public void DoesNotPadTheTrailingCellOfARow()
    {
        // EndDateTime has no struct tag, so its type is the last cell of the row and stays unpadded
        string[] body =
        [
            "\tEndDateTime *string",
            "\tStartDateTime *string \"uriparametername:\\\"startDateTime\\\"\"",
        ];

        var aligned = GoFieldFormatter.AlignFieldBlock(body);

        Assert.Equal(
        [
            "\tEndDateTime   *string",
            "\tStartDateTime *string \"uriparametername:\\\"startDateTime\\\"\"",
        ], aligned);
    }

    [Fact]
    public void DoesNotAlignFieldsSeparatedByComments()
    {
        // typical model struct body: each field carries its own doc comment, so every field is a
        // group of one and none of them get padded
        string[] body =
        [
            "\t// The id property",
            "\tid *string",
            "\t// The displayName property",
            "\tdisplayName *string",
        ];

        Assert.Equal(body, GoFieldFormatter.AlignFieldBlock(body));
    }

    [Fact]
    public void AlignsFlagsEnumConstBlocks()
    {
        string[] body =
        [
            "\tROCK_MOVETYPE = 1",
            "\tPAPER_MOVETYPE = 2",
            "\tSCISSORS_MOVETYPE = 4",
        ];

        var aligned = GoFieldFormatter.AlignFieldBlock(body);

        Assert.Equal(
        [
            "\tROCK_MOVETYPE     = 1",
            "\tPAPER_MOVETYPE    = 2",
            "\tSCISSORS_MOVETYPE = 4",
        ], aligned);
    }

    [Fact]
    public void LeavesIotaConstBlocksUnchanged()
    {
        // only the first row has more than one cell; with no sibling rows sharing its columns
        // there is nothing to pad against, and the bare names are single trailing cells
        string[] body =
        [
            "\tROCK_MOVETYPE MoveType = iota",
            "\tPAPER_MOVETYPE",
            "\tSCISSORS_MOVETYPE",
        ];

        Assert.Equal(body, GoFieldFormatter.AlignFieldBlock(body));
    }

    [Fact]
    public void LeavesAnEmbeddedTypeUnpaddedWithinAGroup()
    {
        // the embedded parent type is a single trailing cell: it neither widens the name column
        // nor gets padded itself, while the fields after it still align with each other
        string[] body =
        [
            "\tSomeVeryLongEmbeddedParentTypeName",
            "\ta *string \"tag\"",
            "\tlonger *int \"tag\"",
        ];

        var aligned = GoFieldFormatter.AlignFieldBlock(body);

        Assert.Equal(
        [
            "\tSomeVeryLongEmbeddedParentTypeName",
            "\ta      *string \"tag\"",
            "\tlonger *int    \"tag\"",
        ], aligned);
    }

    [Fact]
    public void ASingleFieldIsNeverPadded()
    {
        string[] body = ["\tid *string \"tag\""];

        Assert.Equal(body, GoFieldFormatter.AlignFieldBlock(body));
    }
}
