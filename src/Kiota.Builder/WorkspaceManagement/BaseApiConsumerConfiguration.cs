using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kiota.Builder.Configuration;
using Kiota.Builder.Extensions;
using Microsoft.OpenApi.ApiManifest;

namespace Kiota.Builder.WorkspaceManagement;

#pragma warning disable CA2227 // Collection properties should be read only
public abstract class BaseApiConsumerConfiguration
{
    private protected BaseApiConsumerConfiguration()
    {

    }
    private protected BaseApiConsumerConfiguration(GenerationConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);
        DescriptionLocation = config.OpenAPIFilePath;
        IncludePatterns = new HashSet<string>(config.IncludePatterns, StringComparer.OrdinalIgnoreCase);
        ExcludePatterns = new HashSet<string>(config.ExcludePatterns, StringComparer.OrdinalIgnoreCase);
        AllowedExternalOrigins = new HashSet<string>(config.AllowedExternalOrigins, StringComparer.OrdinalIgnoreCase);
        OutputPath = config.OutputPath;
    }
    /// <summary>
    /// The location of the OpenAPI description file.
    /// </summary>
    public string DescriptionLocation { get; set; } = string.Empty;
    /// <summary>
    /// The path patterns for API endpoints to include for this client.
    /// </summary>
    public HashSet<string> IncludePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The path patterns for API endpoints to exclude for this client.
    /// </summary>
    public HashSet<string> ExcludePatterns { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The external origins that can be loaded while resolving references from the OpenAPI description.
    /// </summary>
    public HashSet<string> AllowedExternalOrigins { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    /// <summary>
    /// The output path for the generated code, related to the configuration file.
    /// </summary>
    public string OutputPath { get; set; } = string.Empty;
    public void NormalizeOutputPath(string targetDirectory)
    {
        if (Path.IsPathRooted(OutputPath))
            OutputPath = "./" + Path.GetRelativePath(targetDirectory, OutputPath).NormalizePathSeparators();
        ValidateOutputPath(OutputPath, targetDirectory);
    }
    internal static void ValidateOutputPath(string? outputPath, string targetDirectory)
    {
        ArgumentException.ThrowIfNullOrEmpty(targetDirectory);
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new InvalidOperationException("The output path must be a subdirectory of the workspace.");
        if (IsRootedPath(outputPath) || outputPath.Split('/', '\\').Contains("..", StringComparer.Ordinal))
            throw new InvalidOperationException("The output path must be a relative subdirectory of the workspace and cannot navigate up.");
        var targetFullPath = Path.GetFullPath(targetDirectory);
        var outputFullPath = Path.GetFullPath(Path.Combine(targetFullPath, outputPath));
        var targetFullPathWithSeparator = Path.EndsInDirectorySeparator(targetFullPath) ? targetFullPath : targetFullPath + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!outputFullPath.StartsWith(targetFullPathWithSeparator, comparison))
            throw new InvalidOperationException("The output path must be a subdirectory of the workspace.");
        ValidateNoReparsePointEscape(targetFullPath, outputFullPath, comparison);
    }
    private enum ResolvedOutputPathState
    {
        Valid,
        OutsideWorkspace,
        WorkspaceRoot,
    }
    private static void ValidateNoReparsePointEscape(string targetFullPath, string outputFullPath, StringComparison comparison)
    {
        var state = GetResolvedOutputPathState(targetFullPath, outputFullPath, comparison, false);
        // link targets may use different casing than the on-disk names on case-insensitive file systems, only pay for the lookup when needed
        if (state == ResolvedOutputPathState.OutsideWorkspace && !OperatingSystem.IsWindows())
            state = GetResolvedOutputPathState(targetFullPath, outputFullPath, comparison, true);
        if (state == ResolvedOutputPathState.OutsideWorkspace)
            throw new InvalidOperationException("The output path must be a subdirectory of the workspace and cannot traverse symbolic links or junctions outside of it.");
        if (state == ResolvedOutputPathState.WorkspaceRoot)
            throw new InvalidOperationException("The output path must resolve to a subdirectory of the workspace, not the workspace itself.");
    }
    private static ResolvedOutputPathState GetResolvedOutputPathState(string targetFullPath, string outputFullPath, StringComparison comparison, bool useOnDiskCasing)
    {
        var realTarget = ResolveFinalPath(targetFullPath, useOnDiskCasing);
        var realTargetWithSeparator = Path.EndsInDirectorySeparator(realTarget) ? realTarget : realTarget + Path.DirectorySeparatorChar;
        var relative = Path.GetRelativePath(targetFullPath, outputFullPath);
        var current = realTarget;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                     .Where(static segment => !string.IsNullOrEmpty(segment) && segment != "."))
        {
            current = ResolveFinalPath(Path.Join(current, segment), useOnDiskCasing);
            if (!current.Equals(realTarget, comparison) && !current.StartsWith(realTargetWithSeparator, comparison))
                return ResolvedOutputPathState.OutsideWorkspace;
        }
        return current.Equals(realTarget, comparison) ? ResolvedOutputPathState.WorkspaceRoot : ResolvedOutputPathState.Valid;
    }
    private static string ResolveFinalPath(string path, bool useOnDiskCasing)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var current = Path.GetPathRoot(fullPath) ?? throw new IOException("The output path has no filesystem root.");
            var segments = new Stack<string>(Enumerable.Reverse(fullPath[current.Length..]
                .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)));
            var linkCount = 0;
            while (segments.TryPop(out var segment))
            {
                if (segment == ".")
                    continue;
                if (segment == "..")
                {
                    current = Path.GetDirectoryName(current) ?? current;
                    continue;
                }
                var next = Path.Join(current, segment);
                FileSystemInfo info = Directory.Exists(next) ? new DirectoryInfo(next) : new FileInfo(next);
                var target = info.LinkTarget;
                if (target is null)
                {
                    current = useOnDiskCasing && info.Exists ? Path.Join(current, GetOnDiskName(current, segment)) : next;
                    continue;
                }
                if (++linkCount > 40)
                    throw new IOException("Too many symbolic links in the output path.");
                if (OperatingSystem.IsWindows())
                    target = info.ResolveLinkTarget(false)?.FullName ?? throw new IOException("The output path link could not be resolved.");
                if (Path.IsPathRooted(target))
                {
                    current = Path.GetPathRoot(target) ?? throw new IOException("The output path link has no filesystem root.");
                    target = target[current.Length..];
                }
                foreach (var targetSegment in Enumerable.Reverse(target.Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)))
                    segments.Push(targetSegment);
            }
            return Path.TrimEndingDirectorySeparator(current);
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("The output path could not be resolved and cannot be validated.", ex);
        }
    }
    private static string GetOnDiskName(string directory, string name)
    {
        // the entry exists under the requested name, a single entry differing only by casing means a case-insensitive file system
        try
        {
            string? match = null;
            foreach (var entryName in Directory.EnumerateFileSystemEntries(directory).Select(Path.GetFileName))
            {
                if (string.Equals(entryName, name, StringComparison.Ordinal))
                    return name;
                if (string.Equals(entryName, name, StringComparison.OrdinalIgnoreCase))
                {
                    if (match is not null)
                        return name;
                    match = entryName;
                }
            }
            return match ?? name;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return name;
        }
    }
    private static bool IsRootedPath(string path)
    {
        return Path.IsPathRooted(path) ||
               path.StartsWith(@"\\", StringComparison.Ordinal) ||
               path.StartsWith("//", StringComparison.Ordinal) ||
               path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/');
    }
    public void NormalizeDescriptionLocation(string targetDirectory)
    {
        if (Path.IsPathRooted(DescriptionLocation) && Path.GetFullPath(DescriptionLocation).StartsWith(Path.GetFullPath(targetDirectory), StringComparison.Ordinal) && !DescriptionLocation.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            DescriptionLocation = "./" + Path.GetRelativePath(targetDirectory, DescriptionLocation).NormalizePathSeparators();
    }
    protected void CloneBase(BaseApiConsumerConfiguration target)
    {
        ArgumentNullException.ThrowIfNull(target);
        target.OutputPath = OutputPath;
        target.DescriptionLocation = DescriptionLocation;
        target.IncludePatterns = new HashSet<string>(IncludePatterns, StringComparer.OrdinalIgnoreCase);
        target.ExcludePatterns = new HashSet<string>(ExcludePatterns, StringComparer.OrdinalIgnoreCase);
        target.AllowedExternalOrigins = new HashSet<string>(AllowedExternalOrigins, StringComparer.OrdinalIgnoreCase);
    }
    protected void UpdateGenerationConfigurationFromBase(GenerationConfiguration config, string clientName, IList<RequestInfo>? requests)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(clientName);
        config.IncludePatterns = IncludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.ExcludePatterns = ExcludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.AllowedExternalOrigins = AllowedExternalOrigins.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.OpenAPIFilePath = DescriptionLocation;
        config.OutputPath = OutputPath;
        config.ClientClassName = clientName;
        if (requests is { Count: > 0 })
        {
            config.PatternsOverride = requests.Where(static x => !x.Exclude && !string.IsNullOrEmpty(x.Method) && !string.IsNullOrEmpty(x.UriTemplate))
                                            .Select(static x => $"/{x.UriTemplate}#{x.Method!.ToUpperInvariant()}")
                                            .ToHashSet();
        }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
