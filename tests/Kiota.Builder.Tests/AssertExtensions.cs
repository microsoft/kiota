using System;
using System.Linq;

using Xunit;

namespace Kiota.Builder.Tests;

public static class AssertExtensions
{
    public static void CurlyBracesAreClosed(string generatedCode, int offset = 0)
    {
        if (!string.IsNullOrEmpty(generatedCode))
            Assert.Equal(generatedCode.Count(static x => x == '{'), generatedCode.Count(static x => x == '}') + offset);
    }
    public static void Before(string before, string after, string generatedCode, string start = "", StringComparison comparison = StringComparison.OrdinalIgnoreCase)
    {
        Assert.InRange(generatedCode.IndexOf(before, comparison), string.IsNullOrEmpty(start) ? 0 : generatedCode.IndexOf(start, comparison), generatedCode.IndexOf(after, comparison));
    }
    public static void OutsideOfBlock(string content, string blockOpening, string generatedCode, StringComparison comparison = StringComparison.OrdinalIgnoreCase, string blockOpeningSymbol = "{", string blockClosingSymbol = "}")
    {
        var openingSymbolIndex = generatedCode.IndexOf(blockOpeningSymbol, generatedCode.IndexOf(blockOpening, comparison), comparison);
        var closingSymbolIndex = GetClosingBlockIndex(generatedCode, openingSymbolIndex, blockOpeningSymbol, blockClosingSymbol, comparison);
        Assert.NotInRange(generatedCode.IndexOf(content, comparison), openingSymbolIndex, closingSymbolIndex + 1);
    }
    private static int GetClosingBlockIndex(string generatedCode, int startupLookupIndex, string blockOpeningSymbol, string blockClosingSymbol, StringComparison comparison)
    {
        var closingBlockIndex = generatedCode.IndexOf(blockClosingSymbol, startupLookupIndex, comparison);
        if (closingBlockIndex == -1)
            throw new ArgumentException("The generated code does not contain a closing block symbol");
        if (generatedCode[startupLookupIndex..closingBlockIndex].Contains(blockOpeningSymbol, comparison))
            return GetClosingBlockIndex(generatedCode, closingBlockIndex + 1, blockOpeningSymbol, blockClosingSymbol, comparison);
        return closingBlockIndex;
    }
}
