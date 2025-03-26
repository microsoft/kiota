using System;
using System.IO;
using kiota.Extension;
using Xunit;

namespace Kiota.Tests.Extension;

public class KiotaHostExtensionsTests
{
    public class AcquisitionChannelTests
    {
        [Fact]
        public void ReturnsUnknownForNullOrWhitespacePath()
        {
            Assert.Equal("unknown", KiotaHostExtensions.AcquisitionChannel(null));
            Assert.Equal("unknown", KiotaHostExtensions.AcquisitionChannel(string.Empty));
            Assert.Equal("unknown", KiotaHostExtensions.AcquisitionChannel(" "));
        }

        [Fact]
        public void ReturnsUnknownForUnknownPath()
        {
            Assert.Equal("unknown", KiotaHostExtensions.AcquisitionChannel("/opt/kiota/bin/kiota"));
        }

        [Fact]
        public void ReturnsDockerWhenContainerEnvVarIsSet()
        {
            Environment.SetEnvironmentVariable("KIOTA_CONTAINER", "true");
            Assert.Equal("docker", KiotaHostExtensions.AcquisitionChannel(null));
            Environment.SetEnvironmentVariable("KIOTA_CONTAINER", null);
        }

        [Fact]
        public void ReturnsDotnetToolWhenPathIsGlobalDotnetToolPath()
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Assert.Equal("dotnet_tool", KiotaHostExtensions.AcquisitionChannel(Path.Join(homeDir, ".dotnet", "tools", "kiota.exe")));
        }

        [Fact]
        public void ReturnsAsdfWhenPathIsAsdfDefault()
        {
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                Assert.Equal("asdf", KiotaHostExtensions.AcquisitionChannel(Path.Join(homeDir, ".asdf", "bin", "kiota")));
            }
        }
        [Fact]
        public void ReturnsAsdfWhenPathIsAsdfConfigured()
        {
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                Environment.SetEnvironmentVariable("ASDF_DATA_DIR", Path.Join(homeDir, "custom", ".asdf"));
                Assert.Equal("asdf", KiotaHostExtensions.AcquisitionChannel(Path.Join(homeDir, "custom", ".asdf", "bin", "kiota")));
                Environment.SetEnvironmentVariable("ASDF_DATA_DIR", null);
            }
        }

        [Fact]
        public void ReturnsExtensionWhenPathIsExtensionDefault()
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            Assert.Equal("extension", KiotaHostExtensions.AcquisitionChannel(Path.Join(homeDir, ".vscode", "bin", "kiota")));
        }

        [Fact]
        public void ReturnsHomebrewForMacOsPath()
        {
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                Assert.Equal("homebrew",
                    KiotaHostExtensions.AcquisitionChannel("/opt/homebrew/Cellar/kiota/1.24/bin/kiota"));
            }
        }
    }
}
