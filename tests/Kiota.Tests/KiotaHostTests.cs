using System.CommandLine;
using System.Threading.Tasks;

using kiota;

using Xunit;

namespace Kiota.Tests
{
    public class KiotaHostTests
    {
        [Fact]
        public async Task ThrowsOnInvalidOutputPath() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "-o", "A:\\doesnotexist" });
        }
        [Fact]
        public async Task ThrowsOnInvalidInputPath() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "-d", "A:\\doesnotexist" });
        }
        [Fact]
        public async Task ThrowsOnInvalidInputUrl() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "-d", "https://nonexistentdomain56a535ba-bda6-405e-b5e2-ef5f11bf1003.net/doesnotexist" });
        }
        [Fact]
        public async Task ThrowsOnInvalidLanguage() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "-l", "Pascal" });
        }
        [Fact]
        public async Task ThrowsOnInvalidLogLevel() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "--ll", "Dangerous" });
        }
        [Fact]
        public async Task ThrowsOnInvalidClassName() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "-c", ".Graph" });
        }
        [Fact]
        public async Task AcceptsDeserializers() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "--ds", "Kiota.Tests.TestData.TestDeserializer" });
        }
        [Fact]
        public async Task AcceptsSerializers() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new[] { "-s", "Kiota.Tests.TestData.TestSerializer" });
        }
    }
}
