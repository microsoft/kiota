using System;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Kiota.Builder.Tests.SearchProviders.GitHub.Authentication;

public sealed class TempFolderTokenStorageServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"kiota-token-tests-{Guid.NewGuid():N}");
    private string CacheDirectory => Path.Combine(root, "kiota", "auth");
    private string TokenPath => Path.Combine(CacheDirectory, "test-token.txt");

    private TempFolderTokenStorageService CreateStorage(string fileName = "test-token") => new(root)
    {
        Logger = new Mock<ILogger>().Object,
        FileName = fileName,
    };

    [Fact]
    public async Task StoresReplacesAndDeletesAsync()
    {
        var storage = CreateStorage();
        Assert.Null(await storage.GetTokenAsync(TestContext.Current.CancellationToken));
        Assert.False(await storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
        await storage.SetTokenAsync("first", TestContext.Current.CancellationToken);
        Assert.True(await storage.IsTokenPresentAsync(TestContext.Current.CancellationToken));
        Assert.Equal("first", await storage.GetTokenAsync(TestContext.Current.CancellationToken));
        await storage.SetTokenAsync("replacement", TestContext.Current.CancellationToken);
        Assert.Equal("replacement", await storage.GetTokenAsync(TestContext.Current.CancellationToken));
        Assert.Equal([TokenPath], Directory.GetFiles(CacheDirectory));
        Assert.True(await storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
        Assert.False(await storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task MissingCacheDoesNotCreateDirectoriesAsync()
    {
        var storage = CreateStorage();
        foreach (var directory in new[] { root, Path.Combine(root, "kiota"), CacheDirectory })
        {
            Assert.False(Directory.Exists(directory));
            Assert.Null(await storage.GetTokenAsync(TestContext.Current.CancellationToken));
            Assert.False(await storage.IsTokenPresentAsync(TestContext.Current.CancellationToken));
            Assert.False(await storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
            Assert.False(Directory.Exists(directory));
            TokenCacheSecurity.EnsureDirectory(directory, true);
        }
    }

    [Fact]
    public async Task MissingApplicationDataInReadOnlyDirectoryIsAbsentAsync()
    {
        TokenCacheSecurity.EnsureDirectory(root, true);
        var directory = new DirectoryInfo(root);
        DirectorySecurity originalSecurity = null;
        if (OperatingSystem.IsWindows())
        {
            originalSecurity = directory.GetAccessControl();
            var security = directory.GetAccessControl();
            using var identity = WindowsIdentity.GetCurrent();
            security.AddAccessRule(new FileSystemAccessRule(identity.User!, FileSystemRights.CreateDirectories, AccessControlType.Deny));
            directory.SetAccessControl(security);
        }
        else
            File.SetUnixFileMode(root, UnixFileMode.UserRead | UnixFileMode.UserExecute);
        try
        {
            var applicationDataPath = Path.Combine(root, "application-data");
            var storage = new TempFolderTokenStorageService(applicationDataPath)
            {
                Logger = new Mock<ILogger>().Object,
                FileName = "test-token",
            };
            Assert.Null(await storage.GetTokenAsync(TestContext.Current.CancellationToken));
            Assert.False(await storage.IsTokenPresentAsync(TestContext.Current.CancellationToken));
            Assert.False(await storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
            Assert.False(Directory.Exists(applicationDataPath));
        }
        finally
        {
            if (OperatingSystem.IsWindows())
                directory.SetAccessControl(originalSecurity!);
            else
                File.SetUnixFileMode(root, TokenCacheSecurity.DirectoryMode);
        }
    }

    [Fact]
    public async Task CreatesPrivateDirectoryAndFileAsync()
    {
        await CreateStorage().SetTokenAsync("dummy-token", TestContext.Current.CancellationToken);
        TokenCacheSecurity.ValidateFile(TokenPath);
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            var security = new DirectoryInfo(CacheDirectory).GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access);
            Assert.True(security.AreAccessRulesProtected);
            Assert.Equal(identity.User, security.GetOwner(typeof(SecurityIdentifier)));
            foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                Assert.Equal(identity.User, rule.IdentityReference);
        }
        else
        {
            Assert.Equal(TokenCacheSecurity.DirectoryMode, File.GetUnixFileMode(CacheDirectory));
            Assert.Equal(TokenCacheSecurity.FileMode, File.GetUnixFileMode(TokenPath));
        }
    }

    [Fact]
    public async Task RejectsUnsafeExistingDirectoryWithoutRepairAsync()
    {
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectoryInfo(CacheDirectory).GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.Read, InheritanceFlags.None, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(CacheDirectory).SetAccessControl(security);
        }
        else
            File.SetUnixFileMode(CacheDirectory, TokenCacheSecurity.DirectoryMode | UnixFileMode.OtherRead);
        await Assert.ThrowsAsync<IOException>(() => storage.SetTokenAsync("replacement", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.GetTokenAsync(TestContext.Current.CancellationToken));
        Assert.Equal("original", await File.ReadAllTextAsync(TokenPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectsUnsafeExistingFileWithoutRepairAsync()
    {
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        if (OperatingSystem.IsWindows())
        {
            var security = new FileInfo(TokenPath).GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                FileSystemRights.Read, AccessControlType.Allow));
            new FileInfo(TokenPath).SetAccessControl(security);
        }
        else
            File.SetUnixFileMode(TokenPath, TokenCacheSecurity.FileMode | UnixFileMode.OtherRead);
        await Assert.ThrowsAsync<IOException>(() => storage.GetTokenAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.IsTokenPresentAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.SetTokenAsync("replacement", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
        Assert.Equal("original", await File.ReadAllTextAsync(TokenPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectsMacAclGrantsEvenWithPrivatePosixModesAsync()
    {
        if (!OperatingSystem.IsMacOS())
        {
            Assert.Skip("Darwin ACLs can grant access independently of POSIX mode bits.");
            return;
        }
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        var startInfo = new ProcessStartInfo("chmod") { UseShellExecute = false };
        startInfo.ArgumentList.Add("+a");
        startInfo.ArgumentList.Add("everyone allow read");
        startInfo.ArgumentList.Add(TokenPath);
        using var process = Process.Start(startInfo);
        Assert.NotNull(process);
        await process.WaitForExitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(0, process.ExitCode);
        Assert.Equal(TokenCacheSecurity.FileMode, File.GetUnixFileMode(TokenPath));
        await Assert.ThrowsAsync<IOException>(() => storage.GetTokenAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.SetTokenAsync("replacement", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectsLinkedFileAndPreservesDestinationAsync()
    {
        if (OperatingSystem.IsWindows())
            Assert.Skip("Unix symbolic-link validation; Windows reparse-point coverage uses directory junctions.");
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        var sentinel = Path.Combine(root, "sentinel.txt");
        File.Move(TokenPath, sentinel);
        File.CreateSymbolicLink(TokenPath, sentinel);
        await Assert.ThrowsAsync<IOException>(() => storage.GetTokenAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.SetTokenAsync("replacement", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
        Assert.Equal("original", await File.ReadAllTextAsync(sentinel, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task RejectsLinkedDirectoryAndPreservesDestinationAsync()
    {
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        var destination = Path.Combine(root, "destination");
        Directory.Move(CacheDirectory, destination);
        if (OperatingSystem.IsWindows())
        {
            var startInfo = new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell", "v1.0", "powershell.exe"))
            {
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add("-NoProfile");
            startInfo.ArgumentList.Add("-NonInteractive");
            startInfo.ArgumentList.Add("-Command");
            startInfo.ArgumentList.Add($"New-Item -ItemType Junction -Path '{CacheDirectory.Replace("'", "''", StringComparison.Ordinal)}' -Target '{destination.Replace("'", "''", StringComparison.Ordinal)}' -ErrorAction Stop | Out-Null");
            using var process = Process.Start(startInfo);
            Assert.NotNull(process);
            await process.WaitForExitAsync(TestContext.Current.CancellationToken);
            Assert.Equal(0, process.ExitCode);
        }
        else
            Directory.CreateSymbolicLink(CacheDirectory, destination);
        await Assert.ThrowsAsync<IOException>(() => storage.GetTokenAsync(TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.SetTokenAsync("replacement", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<IOException>(() => storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
        Assert.Equal("original", await File.ReadAllTextAsync(Path.Combine(destination, "test-token.txt"), TestContext.Current.CancellationToken));
        Directory.Delete(CacheDirectory);
    }

    [Fact]
    public async Task FailedReplacementPreservesTokenAndRemovesTemporaryFileAsync()
    {
        if (!OperatingSystem.IsWindows())
            Assert.Skip("Windows file-sharing denial exercises failure after the replacement file has been written.");
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        using var heldFile = new FileStream(TokenPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => storage.SetTokenAsync("replacement", TestContext.Current.CancellationToken));
        Assert.Equal("original", await storage.GetTokenAsync(TestContext.Current.CancellationToken));
        Assert.Equal([TokenPath], Directory.GetFiles(CacheDirectory));
    }

    [Fact]
    public async Task ExpiresWithoutRecursiveDeletionAsync()
    {
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        File.SetLastWriteTimeUtc(TokenPath, DateTime.UtcNow.AddMonths(-7));
        Assert.False(await storage.IsTokenPresentAsync(TestContext.Current.CancellationToken));
        Assert.False(File.Exists(TokenPath));
        await storage.SetTokenAsync("another", TestContext.Current.CancellationToken);
        File.SetLastWriteTimeUtc(TokenPath, DateTime.UtcNow.AddMonths(-7));
        Assert.True(await storage.DeleteTokenAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task EmptyTokensAreNotCachedAsync()
    {
        var storage = CreateStorage();
        await Assert.ThrowsAsync<ArgumentException>(() => storage.SetTokenAsync(string.Empty, TestContext.Current.CancellationToken));
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        await File.WriteAllTextAsync(TokenPath, string.Empty, TestContext.Current.CancellationToken);
        Assert.Null(await storage.GetTokenAsync(TestContext.Current.CancellationToken));
        Assert.False(await storage.IsTokenPresentAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancellationPreservesExistingTokenAsync()
    {
        var storage = CreateStorage();
        await storage.SetTokenAsync("original", TestContext.Current.CancellationToken);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.SetTokenAsync("replacement", cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.GetTokenAsync(cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.DeleteTokenAsync(cancellation.Token));
        Assert.Equal("original", await storage.GetTokenAsync(TestContext.Current.CancellationToken));
        Assert.Equal([TokenPath], Directory.GetFiles(CacheDirectory));
    }

    [Theory]
    [InlineData("../outside")]
    [InlineData("..\\outside")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("token.")]
    [InlineData("token ")]
    public async Task RejectsUnsafeKeysAsync(string key)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => CreateStorage(key).SetTokenAsync("dummy", TestContext.Current.CancellationToken));
        Assert.False(Directory.Exists(root));
    }

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }
}
