using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kiota.Cli.Commons.IO;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class JsonOutputFormatterTest
{
    public class WriteOutputFunction_Should
    {
        private readonly TestConsole _console;

        private const string NewLine = "\n";

        public WriteOutputFunction_Should()
        {
            _console = new TestConsole();
        }

        [Fact]
        public void Write_A_Line_With_String_Content()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "Test content";

            formatter.WriteOutput(content, new JsonOutputFormatterOptions(true));

            Assert.Equal($"{content}{NewLine}", _console.Output);
        }

        [Fact]
        public void Write_Indented_Output_Given_A_Minified_Json_String()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var n = NewLine;

            formatter.WriteOutput(content, new JsonOutputFormatterOptions(true));
            var expected = $"{{{n}  \"a\": 1,{n}  \"b\": \"test\"{n}}}";

            Assert.Equal($"{expected}{n}", _console.Output);
        }

        [Fact]
        public void Write_Minified_Output_Given_A_Minified_Json_String_If_Indentation_Disabled()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "{\"a\": 1, \"b\": \"test\"}";

            formatter.WriteOutput(content, new JsonOutputFormatterOptions(false));
            var expected = $"{content}{NewLine}";

            Assert.Equal(expected, _console.Output);
        }

        [Fact]
        public void Write_A_Line_With_Stream_Content()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "Test content";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

            formatter.WriteOutput(stream, new JsonOutputFormatterOptions(true));

            Assert.Equal($"{content}{NewLine}", _console.Output);
        }

        [Fact]
        public void Write_Indented_Output_Given_A_Minified_Json_Stream()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
            var n = NewLine;

            formatter.WriteOutput(stream, new JsonOutputFormatterOptions(true));
            var expected = $"{{{n}  \"a\": 1,{n}  \"b\": \"test\"{n}}}";

            Assert.Equal($"{expected}{n}", _console.Output);
        }

        [Fact]
        public void Write_Minified_Output_Given_A_Minified_Json_Stream_If_Indentation_Disabled()
        {
            var formatter = new JsonOutputFormatter(_console);
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));

            formatter.WriteOutput(stream, new JsonOutputFormatterOptions(false));
            var expected = $"{content}{NewLine}";

            Assert.Equal(expected, _console.Output);
        }
    }

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
