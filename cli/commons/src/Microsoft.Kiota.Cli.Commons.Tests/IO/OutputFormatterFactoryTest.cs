using System;
using Microsoft.Kiota.Cli.Commons.IO;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class OutputFormatterFactoryTest
{
    public class GetFormatterFunction_Should
    {
        [Theory]
        [InlineData(FormatterType.NONE)]
        public void ThrowException_On_Invalid_FormatterType(FormatterType formatterType)
        {
            var factory = new OutputFormatterFactory();

            Assert.Throws<NotSupportedException>(() => factory.GetFormatter(formatterType));
        }

        [Theory]
        [InlineData(FormatterType.JSON, typeof(JsonOutputFormatter))]
        [InlineData(FormatterType.TABLE, typeof(TableOutputFormatter))]
        [InlineData(FormatterType.TEXT, typeof(TextOutputFormatter))]
        public void Return_OutputFormatter_On_FormatterType(FormatterType formatterType, Type expectedType)
        {
            var factory = new OutputFormatterFactory();

            var formatter = factory.GetFormatter(formatterType);

            Assert.NotNull(formatter);
            Assert.IsType(expectedType, formatter);
        }

        [Theory]
        [InlineData("json", typeof(JsonOutputFormatter))]
        [InlineData("JSON", typeof(JsonOutputFormatter))]
        [InlineData("table", typeof(TableOutputFormatter))]
        [InlineData("TABLE", typeof(TableOutputFormatter))]
        [InlineData("text", typeof(TextOutputFormatter))]
        [InlineData("TEXT", typeof(TextOutputFormatter))]
        public void Return_OutputFormatter_On_FormatterType_String(string formatterType, Type expectedType)
        {
            var factory = new OutputFormatterFactory();

            var formatter = factory.GetFormatter(formatterType);

            Assert.NotNull(formatter);
            Assert.Equal(expectedType, formatter.GetType());
        }
    }
}
