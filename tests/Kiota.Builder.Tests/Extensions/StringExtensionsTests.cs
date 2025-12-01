using Kiota.Builder.Extensions;

using Xunit;

namespace Kiota.Builder.Tests.Extensions;

public class StringExtensionsTests
{
    [Fact]
    public void Defensive()
    {
        Assert.Equal(StringExtensions.GetNamespaceImportSymbol(null), string.Empty);
    }
    [Fact]
    public void ToLowerCase()
    {
        string nString = null;
        Assert.Empty(nString.ToFirstCharacterLowerCase());
        Assert.Equal(string.Empty, string.Empty.ToFirstCharacterLowerCase());
        Assert.Equal("toto", "Toto".ToFirstCharacterLowerCase());
    }
    [Fact]
    public void ToUpperCase()
    {
        string nString = null;
        Assert.Empty(nString.ToFirstCharacterUpperCase());
        Assert.Equal(string.Empty, string.Empty.ToFirstCharacterUpperCase());
        Assert.Equal("Toto", "toto".ToFirstCharacterUpperCase());
    }
    [Fact]
    public void ToCamelCase()
    {
        string nString = null;
        Assert.Empty(nString.ToCamelCase());
        Assert.Equal(string.Empty, string.Empty.ToCamelCase());
        Assert.Equal(string.Empty, "-".ToCamelCase());
        Assert.Equal("toto", "toto".ToCamelCase());
        Assert.Equal("totoCamelCase", "toto-camel-case".ToCamelCase());
        Assert.Equal("totoCamelCase", "toto.camel~case".ToCamelCase('.', '~'));
    }
    [Fact]
    public void ToPascalCase()
    {
        string nString = null;
        Assert.Empty(nString.ToPascalCase());
        Assert.Equal(string.Empty, string.Empty.ToPascalCase());
        Assert.Equal("Toto", "toto".ToPascalCase());
        Assert.Equal("TotoPascalCase", "toto-pascal-case".ToPascalCase());
    }
    [Fact]
    public void ToPascalCaseCustomSeparator()
    {
        string nString = null;
        Assert.Empty(nString.ToPascalCase());
        Assert.Equal(string.Empty, string.Empty.ToPascalCase(new[] { '_' }));
        Assert.Equal("Toto", "toto".ToPascalCase(new[] { '_' }));
        Assert.Equal("TotoPascalCase", "toto_pascal_case".ToPascalCase(new[] { '_' }));
    }
    [Fact]
    public void ReplaceValueIdentifier()
    {
        string nString = null;
        Assert.Empty(nString.ReplaceValueIdentifier());
        Assert.Equal(string.Empty, string.Empty.ReplaceValueIdentifier());
        Assert.Equal("microsoft.graph.message.Content", "microsoft.graph.message.$value".ReplaceValueIdentifier());
    }
    [Fact]
    public void ToSnakeCase()
    {
        string nString = null;
        Assert.Empty(nString.ToSnakeCase());
        Assert.Equal(string.Empty, string.Empty.ToSnakeCase());
        Assert.Equal("toto", "Toto".ToSnakeCase());
        Assert.Equal("microsoft_graph_message_content", "microsoft-Graph-Message-Content".ToSnakeCase());
        Assert.Equal("microsoft_graph_message_content", "microsoftGraphMessageContent".ToSnakeCase());
        Assert.Equal("microsoft_graph_message_content", "microsoft_Graph_Message_Content".ToSnakeCase());
        Assert.Equal("test_value", "testValue<WithStrippedContent".ToSnakeCase());
        Assert.Equal("test", "test<Value".ToSnakeCase());
    }
    [Fact]
    public void NormalizeNameSpaceName()
    {
        string nString = null;
        Assert.Empty(nString.NormalizeNameSpaceName("."));
        Assert.Equal(string.Empty, string.Empty.NormalizeNameSpaceName("."));
        Assert.Equal("Toto", "toto".NormalizeNameSpaceName("-"));
        Assert.Equal("Microsoft_Graph_Message_Content", "microsoft.Graph.Message.Content".NormalizeNameSpaceName("_"));
    }
    [InlineData("\" !#$%&'()*+,./:;<=>?@[]\\^`{}|~-", "plus")]
    [InlineData("unchanged", "unchanged")]
    [InlineData("@odata.changed", "OdataChanged")]
    [InlineData("specialLast@", "specialLast")]
    [InlineData("kebab-cased", "kebabCased")]
    [InlineData("123Spelled", "OneTwoThreeSpelled")]
    [InlineData("+1", "plus_1")]
    [InlineData("+1+", "plus_1_plus")]
    [InlineData("+1+1", "plus_1_plus_1")]
    [InlineData("-1", "minus_1")]
    [InlineData("-1-", "minus_1")]
    [InlineData("-1-1", "minus_11")]
    [InlineData("-", "minus")]
    [InlineData("@", "At")]
    [InlineData("_component", "component")]
    [InlineData("__component", "component")]
    [InlineData("a__b", "a__b")]
    [InlineData("_", "Underscore")]
    [Theory]
    public void CleansUpSymbolNames(string input, string expected)
    {
        Assert.Equal(expected, input.CleanupSymbolName());
    }

    [Fact]
    public void EqualsIgnoreCase()
    {
        string a = null;
        string b = null;
        string c = string.Empty;
        Assert.True(a.EqualsIgnoreCase(b));
        Assert.False(a.EqualsIgnoreCase(c));
        Assert.False(a.EqualsIgnoreCase("Ab"));
        Assert.True("Aa".EqualsIgnoreCase("aa"));
        Assert.False("Aa".EqualsIgnoreCase("Ab"));
        Assert.True("AaAaAa".EqualsIgnoreCase("aaaaaa"));
        Assert.True("".EqualsIgnoreCase(""));
        Assert.True("Joe_Doe".EqualsIgnoreCase("joe_doe"));
    }
}
