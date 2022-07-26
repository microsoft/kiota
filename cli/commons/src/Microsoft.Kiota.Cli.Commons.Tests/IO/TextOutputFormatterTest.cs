using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kiota.Cli.Commons.IO;
using Spectre.Console.Testing;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class TextOutputFormatterTest
{
    public class WriteOutputAsyncFunction_Should
    {
        private readonly TestConsole _console;

        private const string NewLine = "\n";

        public WriteOutputAsyncFunction_Should()
        {
            _console = new TestConsole();
        }

        [Fact]
        public async Task Write_A_Line_With_Short_Stream_Content()
        {
            var formatter = new TextOutputFormatter(_console);
            var content = "Test content";
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

            await formatter.WriteOutputAsync(stream, new OutputFormatterOptions());

            Assert.Equal($"{content}{NewLine}", _console.Output);
        }

        [Fact]
        public async Task Write_A_Line_With_Long_Stream_Content()
        {
            var formatter = new TextOutputFormatter(_console);
            using var fs = File.OpenRead("data/long_text_file.txt");

            await formatter.WriteOutputAsync(fs, new OutputFormatterOptions());

            Assert.StartsWith($"Lorem ipsum", _console.Output);
            Assert.EndsWith($"sed nisi lacus sed.{NewLine}", _console.Output);
        }

        [Fact]
        public async Task Write_A_Line_With_Empty_Stream_Content()
        {
            var formatter = new TextOutputFormatter(_console);
            using var stream = Stream.Null;

            await formatter.WriteOutputAsync(stream, new OutputFormatterOptions());

            Assert.EndsWith($"{NewLine}", _console.Output);
        }
    }
}
