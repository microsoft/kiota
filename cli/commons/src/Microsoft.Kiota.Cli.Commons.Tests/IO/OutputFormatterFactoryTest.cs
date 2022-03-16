using System;
using Microsoft.Kiota.Cli.Commons.IO;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class OutputFormatterFactoryTest
{
    public class InstanceProperty_Should
    {
        [Fact]
        public void ReturnOutputFormatterFactoryInstance()
        {
            var instance = new OutputFormatterFactory();

            Assert.NotNull(instance);
        }
    }

    public class GetFormatterFunction_Should
    {
        [Theory]
        [InlineData(FormatterType.NONE)]
        public void ThrowException_On_Invalid_FormatterType(FormatterType formatterType)
        {
            var factory = new OutputFormatterFactory();

            Assert.Throws<NotSupportedException>(() => factory.GetFormatter(formatterType));
        }

        [Fact]
        public void Return_JsonOutputFormatter_On_JSON_FormatterType()
        {
            var factory = new OutputFormatterFactory();

            var formatter = factory.GetFormatter(FormatterType.JSON);

            Assert.NotNull(formatter);
            Assert.True(formatter is JsonOutputFormatter);
        }

        [Fact]
        public void Return_JsonOutputFormatter_On_JSON_String()
        {
            var factory = new OutputFormatterFactory();

            var formatter = factory.GetFormatter("json");

            Assert.NotNull(formatter);
            Assert.True(formatter is JsonOutputFormatter);
        }
    }
}
