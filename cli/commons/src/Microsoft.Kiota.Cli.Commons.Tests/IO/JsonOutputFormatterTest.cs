using System;
using System.CommandLine;
using System.IO;
using System.Text;
using Microsoft.Kiota.Cli.Commons.IO;
using Moq;
using Spectre.Console;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class JsonOutputFormatterTest
{
    public class WriteOutputFunction_Should
    {
        [Fact]
        public void Write_A_Line_With_String_Content()
        {
            var formatter = new JsonOutputFormatter();
            var content = "Test content";
            var stringWriter = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(stringWriter) });
            AnsiConsole.Console = console;

            formatter.WriteOutput(content, new JsonOutputFormatterOptions(true));

            Assert.Equal($"{content}{Environment.NewLine}", stringWriter.ToString());
        }

        [Fact]
        public void Write_Indented_Output_Given_A_Minified_Json_String()
        {
            var formatter = new JsonOutputFormatter();
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stringWriter = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(stringWriter) });
            AnsiConsole.Console = console;
            var n = Environment.NewLine;

            formatter.WriteOutput(content, new JsonOutputFormatterOptions(true));
            var expected = $"{{{n}  \"a\": 1,{n}  \"b\": \"test\"{n}}}";

            Assert.Equal($"{expected}{n}", stringWriter.ToString());
        }

        [Fact]
        public void Write_Minified_Output_Given_A_Minified_Json_String_If_Indentation_Disabled()
        {
            var formatter = new JsonOutputFormatter();
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stringWriter = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(stringWriter) });
            AnsiConsole.Console = console;

            formatter.WriteOutput(content, new JsonOutputFormatterOptions(false));
            var expected = $"{content}{Environment.NewLine}";

            Assert.Equal(expected, stringWriter.ToString());
        }

        [Fact]
        public void Write_A_Line_With_Stream_Content()
        {
            var formatter = new JsonOutputFormatter();
            var content = "Test content";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
            var stringWriter = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(stringWriter) });
            AnsiConsole.Console = console;

            formatter.WriteOutput(stream, new JsonOutputFormatterOptions(true));

            Assert.Equal($"{content}{Environment.NewLine}", stringWriter.ToString());
        }

        [Fact]
        public void Write_Indented_Output_Given_A_Minified_Json_Stream()
        {
            var formatter = new JsonOutputFormatter();
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
            var stringWriter = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(stringWriter) });
            AnsiConsole.Console = console;
            var n = Environment.NewLine;

            formatter.WriteOutput(stream, new JsonOutputFormatterOptions(true));
            var expected = $"{{{n}  \"a\": 1,{n}  \"b\": \"test\"{n}}}";

            Assert.Equal($"{expected}{n}", stringWriter.ToString());
        }

        [Fact]
        public void Write_Minified_Output_Given_A_Minified_Json_Stream_If_Indentation_Disabled()
        {
            var formatter = new JsonOutputFormatter();
            var content = "{\"a\": 1, \"b\": \"test\"}";
            var stream = new MemoryStream(Encoding.ASCII.GetBytes(content));
            var stringWriter = new StringWriter();
            var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(stringWriter) });
            AnsiConsole.Console = console;

            formatter.WriteOutput(stream, new JsonOutputFormatterOptions(false));
            var expected = $"{content}{Environment.NewLine}";

            Assert.Equal(expected, stringWriter.ToString());
        }
    }
}
