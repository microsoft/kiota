﻿using Xunit;

namespace Kiota.Builder.Extensions.Tests {
    public class StringExtensionsTests {
        [Fact]
        public void Defensive() {
            Assert.Equal(StringExtensions.GetNamespaceImportSymbol((string)null), string.Empty);
        }
        [Fact]
        public void ToLowerCase() {
            string nString = null;
            Assert.Null(nString.ToFirstCharacterLowerCase());
            Assert.Equal(string.Empty, string.Empty.ToFirstCharacterLowerCase());
            Assert.Equal("toto", "Toto".ToFirstCharacterLowerCase());
        }
        [Fact]
        public void ToUpperCase() {
            string nString = null;
            Assert.Null(nString.ToFirstCharacterUpperCase());
            Assert.Equal(string.Empty, string.Empty.ToFirstCharacterUpperCase());
            Assert.Equal("Toto", "toto".ToFirstCharacterUpperCase());
        }
        [Fact]
        public void ToCamelCase() {
            string nString = null;
            Assert.Null(nString.ToCamelCase());
            Assert.Equal(string.Empty, string.Empty.ToCamelCase());
            Assert.Equal(string.Empty, "-".ToCamelCase());
            Assert.Equal("toto", "toto".ToCamelCase());
            Assert.Equal("totoCamelCase", "toto-camel-case".ToCamelCase());
            Assert.Equal("totoCamelCase", "toto.camel~case".ToCamelCase(".", "~"));
        }
        [Fact]
        public void ToPascalCase() {
            string nString = null;
            Assert.Null(nString.ToPascalCase());
            Assert.Equal(string.Empty, string.Empty.ToPascalCase());
            Assert.Equal("Toto", "toto".ToPascalCase());
            Assert.Equal("TotoPascalCase", "toto-pascal-case".ToPascalCase());
        }
        [Fact]
        public void ReplaceValueIdentifier() {
            string nString = null;
            Assert.Null(nString.ReplaceValueIdentifier());
            Assert.Equal(string.Empty, string.Empty.ReplaceValueIdentifier());
            Assert.Equal("microsoft.graph.message.Content", "microsoft.graph.message.$value".ReplaceValueIdentifier());
        }
        [Fact]
        public void ToSnakeCase() {
            string nString = null;
            Assert.Null(nString.ToSnakeCase());
            Assert.Equal(string.Empty, string.Empty.ToSnakeCase());
            Assert.Equal("toto", "Toto".ToSnakeCase());
            Assert.Equal("microsoft_graph_message_content", "microsoft-Graph-Message-Content".ToSnakeCase());
            Assert.Equal("microsoft_graph_message_content", "microsoftGraphMessageContent".ToSnakeCase());
        }
        [Fact]
        public void NormalizeNameSpaceName() {
            string nString = null;
            Assert.Null(nString.NormalizeNameSpaceName("."));
            Assert.Equal(string.Empty, string.Empty.NormalizeNameSpaceName("."));
            Assert.Equal("Toto", "toto".NormalizeNameSpaceName("-"));
            Assert.Equal("Microsoft_Graph_Message_Content", "microsoft.Graph.Message.Content".NormalizeNameSpaceName("_"));
        }
        [InlineData("\" !#$%&'()*+,./:;<=>?@[]\\^`{}|~-", "")]
        [InlineData("unchanged", "unchanged")]
        [InlineData("@odata.changed", "OdataChanged")]
        [InlineData("specialLast@", "specialLast")]
        [InlineData("kebab-cased", "kebabCased")]
        [Theory]
        public void CleansUpSymbolNames(string input, string expected) {
            Assert.Equal(expected, input.CleanupSymbolName());
        }
    }
    
}
