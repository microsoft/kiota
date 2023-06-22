using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Kiota.Builder.Configuration;
using Kiota.Builder.SearchProviders.GitHub.Authentication;
using Kiota.Builder.SearchProviders.GitHub.GitHubClient;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace Kiota.Builder;

public class UpdateService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private readonly UpdateConfiguration _updateConfiguration;
    private readonly VersionComparer _versionComparer = new();
    private static readonly string _lastVerificationFilePath = Path.Combine(Path.GetTempPath(), Constants.TempDirectoryName, "update", "timestamp.txt");
    public UpdateService(HttpClient httpClient, ILogger logger, UpdateConfiguration updateConfiguration)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(updateConfiguration);
        _httpClient = httpClient;
        _logger = logger;
        _updateConfiguration = updateConfiguration;
    }
    public async Task<string> GetUpdateMessageAsync(string currentVersion, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(currentVersion) || _updateConfiguration.Disabled)
            return string.Empty;
        try
        {
            var lastVerificationDate = await GetLastVerificationDateAsync(cancellationToken).ConfigureAwait(false);
            if (!ShouldCheckForUpdates(lastVerificationDate)) return string.Empty;
            using var requestAdapter = new HttpClientRequestAdapter(new AnonymousAuthenticationProvider(), httpClient: _httpClient);
            var gitHubClient = new GitHubClient(requestAdapter);
            var releases = await gitHubClient.Repos[_updateConfiguration.OrgName][_updateConfiguration.RepoName].Releases.GetAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (releases is null) return string.Empty;
            var latestVersion = releases.Where(static x => x.Draft == false && x.Prerelease == false && !string.IsNullOrEmpty(x.TagName))
                                        .Select(static x => GetVersionFromLabel(x.TagName!))
                                        .OfType<Version>()
                                        .OrderDescending(_versionComparer)
                                        .FirstOrDefault();
            if (latestVersion is null) return string.Empty;
            var currentVersionParsed = GetVersionFromLabel(currentVersion);
            if (currentVersionParsed is null) return string.Empty;
            await SetLastVerificationDateAsync(DateTimeOffset.UtcNow, cancellationToken).ConfigureAwait(false);
            if (_versionComparer.Compare(currentVersionParsed, latestVersion) < 0)
                return $"A newer version of Kiota ({latestVersion}) is available. You are currently using version {currentVersion}. https://aka.ms/get/kiota";
        }
#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogWarning(ex, "Unable to check for updates");
        }
        return string.Empty;
    }
    private static async Task<DateTimeOffset> GetLastVerificationDateAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_lastVerificationFilePath))
        {
            var lastVerificationDate = await File.ReadAllTextAsync(_lastVerificationFilePath, cancellationToken).ConfigureAwait(false);
            if (DateTimeOffset.TryParse(lastVerificationDate, out var parsedDate))
                return parsedDate;
        }
        return DateTimeOffset.MinValue;

    }
    private static async Task SetLastVerificationDateAsync(DateTimeOffset date, CancellationToken cancellationToken)
    {
        ClearVerificationDate();
        var directoryPath = Path.GetDirectoryName(_lastVerificationFilePath);
        if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            Directory.CreateDirectory(directoryPath);
        await File.WriteAllTextAsync(_lastVerificationFilePath, date.ToString("o"), cancellationToken).ConfigureAwait(false);
    }
    internal static void ClearVerificationDate()
    {
        if (File.Exists(_lastVerificationFilePath))
            File.Delete(_lastVerificationFilePath);
    }
    private static bool ShouldCheckForUpdates(DateTimeOffset lastVerificationDate)
    {
        return lastVerificationDate < DateTimeOffset.UtcNow.AddHours(-1);
    }
    private static Version? GetVersionFromLabel(string label)
    {
        if (string.IsNullOrEmpty(label)) return null;
        var versionLabel = label.TrimStart('v', 'V').Split('-', StringSplitOptions.RemoveEmptyEntries).First();
        if (Version.TryParse(versionLabel, out var parsedVersion))
            return parsedVersion;
        return null;
    }
}

internal class VersionComparer : IComparer<Version>
{
    public int Compare(Version? x, Version? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => -1,
            (_, null) => 1,
            (_, _) => x!.CompareTo(y!)
        };
    }
}
