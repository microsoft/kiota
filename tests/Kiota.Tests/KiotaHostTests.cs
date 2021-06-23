using System;
using Xunit;
using System.Threading.Tasks;
using System.CommandLine;

namespace Kiota.Tests
{
    public class KiotaHostTests
    {
        [Fact]
        public async Task ThrowsOnInvalidOutputPath() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new string[] { "-o", "A:\\doesnotexist" });
        }
        [Fact]
        public async Task ThrowsOnInvalidInputPath() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new string[] { "-d", "A:\\doesnotexist" });
        }
        [Fact]
        public async Task ThrowsOnInvalidInputUrl() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new string[] { "-d", "https://nonexistentdomain56a535ba-bda6-405e-b5e2-ef5f11bf1003.net/doesnotexist" });
        }
        [Fact]
        public async Task ThrowsOnInvalidLanguage() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new string[] { "-l", "Pascal" });
        }
        [Fact]
        public async Task ThrowsOnInvalidLogLevel() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new string[] { "--ll", "Dangerous" });
        }
        [Fact]
        public async Task ThrowsOnInvalidClassName() {
            await new KiotaHost().GetRootCommand().InvokeAsync(new string[] { "-c", ".Graph" });
        }
    }
}
