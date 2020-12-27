using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace kiota.core.tests
{
    public class KiotaBuilderTests
    {
        [Fact]
        public void Single_root_node_creates_single_request_builder_class()
        {
            var node = new OpenApiUrlSpaceNode("");
            var builder = new KiotaBuilder(new MockLogger(), new GenerationConfiguration() { ClientClassName = "Graph" });
            var codeModel = builder.CreateSourceModel(node);

            Assert.Equal(1, codeModel.InnerChildElements.Count);
        }
    }

    public class MockLogger : ILogger<KiotaBuilder>
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return new MemoryStream(); // Something harmless that implements IDisposable
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return false;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            //NOOP
        }
    }
}
