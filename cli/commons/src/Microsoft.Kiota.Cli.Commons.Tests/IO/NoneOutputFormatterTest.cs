using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kiota.Cli.Commons.IO;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class NoneOutputFormatterTest
{
    public class WriteOutputAsyncFunction_Should
    {
        [Fact]
        public async Task Write_No_Content_With_Short_Stream_Content()
        {
            var formatter = new NoneOutputFormatter();
            var content = "Test content";
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
            var sb = new StringBuilder();
            using var outWriter = new StringWriter(sb);
            using var errorWriter = new StringWriter(sb);
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);

            await formatter.WriteOutputAsync(stream, new OutputFormatterOptions());

            Assert.Equal(0, sb.Length);
        }

        [Fact]
        public async Task Write_Nothing_Given_A_Null_Stream()
        {
            var formatter = new NoneOutputFormatter();
            Stream? stream = null;

            var sb = new StringBuilder();
            using var outWriter = new StringWriter(sb);
            using var errorWriter = new StringWriter(sb);
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);

            await formatter.WriteOutputAsync(stream, new OutputFormatterOptions());

            Assert.Equal(0, sb.Length);
        }
    }
}
