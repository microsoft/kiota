using System.Linq;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.Models;
using Xunit;

namespace Kiota.Builder.Tests.Extensions;
public class OpenApiResponsesExtensionsTests
{
    [Fact]
    public void WhereValidForMediaTypeSelectionReturnsAllIfNothingToFilterOut()
    {
        var sut = CreateWithStatusCodes(["100", "200", "201", "202", "204", "304", "307"]);
        var result = sut.WhereValidForMediaTypeSelection().ToList();
        Assert.Equal(7, result.Count);
        Assert.Equal(["100", "200", "201", "202", "204", "304", "307"], result.Select(r => r.Key));
    }

    [Fact]
    public void WhereValidForMediaTypeSelectionRemoves400RangeEntries()
    {
        var sut = CreateWithStatusCodes(["200", "304", "400", "469", "499", "4XX"]);
        var result = sut.WhereValidForMediaTypeSelection().ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal(["200", "304"], result.Select(r => r.Key));
    }

    [Fact]
    public void WhereValidForMediaTypeSelectionRemoves500RangeEntries()
    {
        var sut = CreateWithStatusCodes(["200", "304", "500", "569", "599", "5XX"]);
        var result = sut.WhereValidForMediaTypeSelection().ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal(["200", "304"], result.Select(r => r.Key));
    }

    [Fact]
    public void WhereValidForMediaTypeSelectionRemovesDefaultEntry()
    {
        var sut = CreateWithStatusCodes(["200", "304", "default"]);
        var result = sut.WhereValidForMediaTypeSelection().ToList();
        Assert.Equal(2, result.Count);
        Assert.Equal(["200", "304"], result.Select(r => r.Key));
    }

    private OpenApiResponses CreateWithStatusCodes(string[] statusCodes)
    {
        var responses = new OpenApiResponses();
        foreach (var sc in statusCodes)
        {
            responses.Add(sc, new OpenApiResponse());
        }
        return responses;
    }
}
