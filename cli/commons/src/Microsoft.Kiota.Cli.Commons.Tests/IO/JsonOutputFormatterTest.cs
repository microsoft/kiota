using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kiota.Cli.Commons.IO;
using Spectre.Console.Testing;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class JsonOutputFormatterTest
{
    public class WriteOutputAsyncFunction_Should
    {
        private readonly TestConsole _console;

        private const string _newLine = "\n";

        public WriteOutputAsyncFunction_Should()
        {
            _console = new TestConsole();
        }

        [Fact]
        public async Task Write_A_Line_With_Stream_Content()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "Test content";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

            await formatter.WriteOutputAsync(stream, new JsonOutputFormatterOptions(true));

            Assert.Equal($"{content}{_newLine}", _console.Output);
        }

        [Fact]
        public async Task Write_Indented_Output_Given_A_Minified_Json_Stream()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
            var n = _newLine;

            await formatter.WriteOutputAsync(stream, new JsonOutputFormatterOptions(true));
            var expected = $"{{{n}  \"a\": 1,{n}  \"b\": \"test\"{n}}}";

            Assert.Equal($"{expected}{n}", _console.Output);
        }

        [Fact]
        public async Task Write_Minified_Output_Given_A_Minified_Json_Stream_If_Indentation_Disabled()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

            await formatter.WriteOutputAsync(stream, new JsonOutputFormatterOptions(false));
            var expected = $"{content}{_newLine}";

            Assert.Equal(expected, _console.Output);
        }
    }
}
