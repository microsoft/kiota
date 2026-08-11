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
    }
    protected void UpdateGenerationConfigurationFromBase(GenerationConfiguration config, string clientName, IList<RequestInfo>? requests)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrEmpty(clientName);
        config.IncludePatterns = IncludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.ExcludePatterns = ExcludePatterns.ToHashSet(StringComparer.OrdinalIgnoreCase);
        config.OpenAPIFilePath = DescriptionLocation;
        config.OutputPath = OutputPath;
        config.ClientClassName = clientName;
        config.Serializers.Clear();
        config.Deserializers.Clear();
        if (requests is { Count: > 0 })
        {
            config.PatternsOverride = requests.Where(static x => !x.Exclude && !string.IsNullOrEmpty(x.Method) && !string.IsNullOrEmpty(x.UriTemplate))
                                            .Select(static x => $"/{x.UriTemplate}#{x.Method!.ToUpperInvariant()}")
                                            .ToHashSet();
        }
    }
}
#pragma warning restore CA2227 // Collection properties should be read only
