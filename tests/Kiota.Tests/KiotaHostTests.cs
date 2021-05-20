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
            await KiotaHost.GetRootCommand().InvokeAsync(new string[] { "-o", "X:\\doesnexit" });
        }
        [Fact]
        public async Task ThrowsOnInvalidInputPath() {
            await KiotaHost.GetRootCommand().InvokeAsync(new string[] { "-d", "X:\\doesnexit" });
            await KiotaHost.GetRootCommand().InvokeAsync(new string[] { "-d", "https://nonexistentdomain.net/doesnexit" });
        }
    }
}
