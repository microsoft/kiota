using System;
using System.CommandLine;
using System.IO;
using System.Text;
using DevLab.JmesPath;
using Microsoft.Kiota.Cli.Commons.IO;
using Moq;
using Spectre.Console;
using Xunit;

namespace Microsoft.Kiota.Cli.Commons.Tests.IO;

public class JmesPathOutputFilterTest
{
    public class FilterOutputFunction_Should
    {
        [Fact]
        public void Call_JmesPath_Transform_Function()
        {
            var jmesPath = new JmesPath();
            var filter = new JmesPathOutputFilter(jmesPath);

            var result = filter.FilterOutput("{\"a\": 1, \"b\": true}", "a");

            Assert.Equal("1", result);
        }
    }
}
