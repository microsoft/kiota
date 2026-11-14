using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Kiota.Builder.SearchProviders.GitHub.Authentication;

[SuppressMessage("Interoperability", "CA2101:Specify marshaling for P/Invoke string arguments", Justification = "Unix paths explicitly use LPUTF8Str rather than platform ANSI marshaling.")]
internal static class TokenCacheSecurity
{
    internal const UnixFileMode DirectoryMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    internal const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    internal static void EnsureDirectory(string path, bool privateDirectory)
    {
        if (OperatingSystem.IsWindows())
        {
            if (!Directory.Exists(path))
            {
                var security = new DirectorySecurity();
                security.SetOwner(CurrentUser);
                security.SetAccessRuleProtection(true, false);
                security.AddAccessRule(new FileSystemAccessRule(CurrentUser, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
                new DirectoryInfo(path).Create(security);
            }
            ValidateWindows(new DirectoryInfo(path).GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access), privateDirectory);
        }
        else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            Directory.CreateDirectory(path, DirectoryMode);
            ValidateUnix(GetUnixStatus(path), true, privateDirectory);
        }
        else
            throw new PlatformNotSupportedException("Private credential storage is not supported on this platform.");

        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
            throw UnsafeStorage();
    }

    internal static void ValidateFile(string path)
    {
        var attributes = File.GetAttributes(path);
        if ((attributes & (FileAttributes.ReparsePoint | FileAttributes.Directory)) != 0)
            throw UnsafeStorage();
        if (OperatingSystem.IsWindows())
            ValidateWindows(new FileInfo(path).GetAccessControl(AccessControlSections.Owner | AccessControlSections.Access), true);
        else
            ValidateUnix(GetUnixStatus(path), false, true);
    }

    [SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "The returned FileStream owns the native SafeFileHandle; failure paths dispose it.")]
    internal static FileStream OpenRead(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var stream = new FileStream(path, System.IO.FileMode.Open, FileAccess.Read, FileShare.Read);
            try
            {
                ValidateWindows(stream.GetAccessControl(), true);
                return stream;
            }
            catch
            {
                stream.Dispose();
                throw;
            }
        }

        // O_NOFOLLOW rejects the final link; the validated private parent prevents cross-account path replacement.
        var flags = OperatingSystem.IsLinux() ? 0x20000 | 0x80000 : 0x100 | 0x1000000;
        var descriptor = Open(path, flags);
        if (descriptor < 0)
            throw new IOException("Unable to open the private credential cache.", new Win32Exception(Marshal.GetLastPInvokeError()));
        var handle = new SafeFileHandle((IntPtr)descriptor, true);
        try
        {
            ValidateUnix(GetUnixStatus(handle), false, true);
            return new FileStream(handle, FileAccess.Read);
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    private static void ValidateUnix(UnixStatus status, bool directory, bool privateEntry)
    {
        var expectedType = directory ? 0x4000 : 0x8000;
        var permissions = status.Mode & 0xFFF;
        if (status.UserId != GetEffectiveUserId() ||
            (status.Mode & 0xF000) != expectedType ||
            (!directory && status.LinkCount != 1) ||
            (privateEntry && permissions != (int)(directory ? DirectoryMode : FileMode)) ||
            (!privateEntry && (permissions & 0xC12) != 0))
            throw UnsafeStorage();
    }

    [SupportedOSPlatform("windows")]
    private static SecurityIdentifier CurrentUser
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User ?? throw new IOException("Unable to identify the current user for private credential storage.");
        }
    }

    [SupportedOSPlatform("windows")]
    private static void ValidateWindows(FileSystemSecurity security, bool privateEntry)
    {
        if (!CurrentUser.Equals(security.GetOwner(typeof(SecurityIdentifier))))
            throw UnsafeStorage();
        foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
        {
            if (rule.AccessControlType != AccessControlType.Allow ||
                CurrentUser.Equals(rule.IdentityReference) ||
                rule.IdentityReference is SecurityIdentifier sid &&
                (sid.IsWellKnown(WellKnownSidType.LocalSystemSid) || sid.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)))
                continue;
            var prohibited = privateEntry ? FileSystemRights.FullControl :
                FileSystemRights.Write | FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles |
                FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;
            if ((rule.FileSystemRights & prohibited) != 0)
                throw UnsafeStorage();
        }
    }

    private static IOException UnsafeStorage() => new("The credential cache has unsafe ownership, permissions, or links. Use a private per-user application-data directory and sign in again.");

    private readonly record struct UnixStatus(uint UserId, int Mode, uint LinkCount);

    private static UnixStatus GetUnixStatus(string path)
    {
        if (OperatingSystem.IsLinux())
            return GetLinuxStatus(-100, path, 0x100);
        if (OperatingSystem.IsMacOS())
        {
            var result = RuntimeInformation.ProcessArchitecture == Architecture.X64 ?
                MacLStat64(path, out var status) : MacLStat(path, out status);
            if (result != 0)
                throw new IOException("Unable to validate credential cache ownership.", new Win32Exception(Marshal.GetLastPInvokeError()));
            ValidateMacAcl(MacGetAcl(path, 0x100));
            return new(status.UserId, status.Mode, status.LinkCount);
        }
        throw new PlatformNotSupportedException();
    }

    private static UnixStatus GetUnixStatus(SafeFileHandle handle)
    {
        if (OperatingSystem.IsLinux())
            return GetLinuxStatus(handle.DangerousGetHandle().ToInt32(), string.Empty, 0x1000);
        var result = RuntimeInformation.ProcessArchitecture == Architecture.X64 ?
            MacFStat64(handle, out var status) : MacFStat(handle, out status);
        if (result != 0)
            throw new IOException("Unable to validate the opened credential cache.", new Win32Exception(Marshal.GetLastPInvokeError()));
        ValidateMacAcl(MacGetFdAcl(handle, 0x100));
        return new(status.UserId, status.Mode, status.LinkCount);
    }

    private static void ValidateMacAcl(IntPtr acl)
    {
        if (acl == IntPtr.Zero)
        {
            // Darwin reports ENOENT when the validated object has no extended ACL.
            if (Marshal.GetLastPInvokeError() == 2)
                return;
            throw new IOException("Unable to validate credential cache access controls.", new Win32Exception(Marshal.GetLastPInvokeError()));
        }
        try
        {
            var entryId = 0;
            int result;
            while ((result = MacGetAclEntry(acl, entryId, out var entry)) == 0)
            {
                entryId = -1;
                // Darwin ACL grants can override owner-only POSIX modes. Fail closed on any additional grant.
                if (MacGetAclTag(entry, out var tag) != 0 || tag == 1)
                    throw UnsafeStorage();
            }
            if (result != -1 || Marshal.GetLastPInvokeError() != 22)
                throw new IOException("Unable to enumerate credential cache access controls.");
        }
        finally
        {
            _ = MacFreeAcl(acl);
        }
    }

    private static UnixStatus GetLinuxStatus(int descriptor, string path, int flags)
    {
        try
        {
            if (LinuxStatX(descriptor, path, flags, 0x1F, out var status) != 0 || (status.Mask & 0x1F) != 0x1F)
                throw new IOException("Unable to validate credential cache ownership.", new Win32Exception(Marshal.GetLastPInvokeError()));
            return new(status.UserId, status.Mode, status.LinkCount);
        }
        catch (EntryPointNotFoundException ex)
        {
            throw new PlatformNotSupportedException("Private credential storage requires Linux statx support.", ex);
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 256)]
    private struct LinuxStatus
    {
        [FieldOffset(0)] internal uint Mask;
        [FieldOffset(16)] internal uint LinkCount;
        [FieldOffset(20)] internal uint UserId;
        [FieldOffset(28)] internal ushort Mode;
    }

    [StructLayout(LayoutKind.Explicit, Size = 144)]
    private struct MacStatus
    {
        [FieldOffset(4)] internal ushort Mode;
        [FieldOffset(6)] internal ushort LinkCount;
        [FieldOffset(16)] internal uint UserId;
    }

    [DllImport("libc", EntryPoint = "geteuid")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern uint GetEffectiveUserId();

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int Open([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags);

    [DllImport("libc", EntryPoint = "statx", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int LinuxStatX(int descriptor, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, int flags, uint mask, out LinuxStatus status);

    [DllImport("libc", EntryPoint = "lstat", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacLStat([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out MacStatus status);

    [DllImport("libc", EntryPoint = "lstat$INODE64", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacLStat64([MarshalAs(UnmanagedType.LPUTF8Str)] string path, out MacStatus status);

    [DllImport("libc", EntryPoint = "fstat", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacFStat(SafeFileHandle handle, out MacStatus status);

    [DllImport("libc", EntryPoint = "fstat$INODE64", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacFStat64(SafeFileHandle handle, out MacStatus status);

    [DllImport("libc", EntryPoint = "acl_get_file", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern IntPtr MacGetAcl([MarshalAs(UnmanagedType.LPUTF8Str)] string path, int type);

    [DllImport("libc", EntryPoint = "acl_get_fd_np", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern IntPtr MacGetFdAcl(SafeFileHandle handle, int type);

    [DllImport("libc", EntryPoint = "acl_get_entry", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacGetAclEntry(IntPtr acl, int entryId, out IntPtr entry);

    [DllImport("libc", EntryPoint = "acl_get_tag_type", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacGetAclTag(IntPtr entry, out int tag);

    [DllImport("libc", EntryPoint = "acl_free")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int MacFreeAcl(IntPtr acl);
}
