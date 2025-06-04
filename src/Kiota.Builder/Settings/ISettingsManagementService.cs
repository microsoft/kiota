using System.Threading;
using System.Threading.Tasks;
using Microsoft.OpenApi;

namespace Kiota.Builder.Settings;
/// <summary>
/// A service that manages the settings file for http language snippets.
/// </summary>
public interface ISettingsManagementService
{
    /// <summary>
    /// Gets the settings file for a Kiota project by crawling the directory tree.
    /// </summary>
    /// <param name="searchDirectory"></param>
    /// <returns></returns>
    string? GetDirectoryContainingSettingsFile(string searchDirectory);

    /// <summary>
    /// Writes the settings file to a directory.
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <param name="openApiDocument">OpenApi document</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task WriteSettingsFileAsync(string directoryPath, OpenApiDocument openApiDocument, CancellationToken cancellationToken);
}
