using System;
using System.IO;
using System.Threading.Tasks;
using kiota;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Kiota.Tests;

[CollectionDefinition("Plugin workspace", DisableParallelization = true)]
public sealed class PluginWorkspaceCollection;

[Collection("Plugin workspace")]
public sealed class PluginRefreshTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RefreshPreservesSharedOutputAsync(bool singlePlugin)
    {
        var directory = Directory.CreateTempSubdirectory();
        var previousDirectory = Directory.GetCurrentDirectory();
        var previousUpdateSetting = Environment.GetEnvironmentVariable("KIOTA_UPDATE__DISABLED");
        try
        {
            Directory.SetCurrentDirectory(directory.FullName);
            Environment.SetEnvironmentVariable("KIOTA_UPDATE__DISABLED", "true");
            await File.WriteAllTextAsync("description.yml", """
                openapi: 3.0.3
                info:
                  title: Shared plugins
                  version: 1.0.0
                servers:
                  - url: https://example.test
                paths:
                  /users:
                    get:
                      operationId: listUsers
                      responses:
                        '200':
                          description: Success
                          content:
                            application/json:
                              schema:
                                type: string
                """, TestContext.Current.CancellationToken);
            using var services = new ServiceCollection().BuildServiceProvider();
            foreach (var name in new[] { "First", "Second", "Third" })
                Assert.Equal(0, await KiotaPluginCommands.GetPluginNodeCommand(services)
                    .Parse(["add", "--plugin-name", name, "-d", "description.yml", "-t", "APIPlugin", "-o", "output"])
                    .InvokeAsync(cancellationToken: TestContext.Current.CancellationToken));
            await File.WriteAllTextAsync("output/notes.txt", "keep", TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync("output/first-openapi.yml", "stale", TestContext.Current.CancellationToken);
            string[] arguments = singlePlugin ? ["generate", "--refresh", "--plugin-name", "First"] : ["generate", "--refresh"];
            Assert.Equal(0, await KiotaPluginCommands.GetPluginNodeCommand(services).Parse(arguments)
                .InvokeAsync(cancellationToken: TestContext.Current.CancellationToken));
            foreach (var name in new[] { "first", "second", "third" })
            {
                Assert.True(File.Exists($"output/{name}-apiplugin.json"));
                Assert.True(File.Exists($"output/{name}-openapi.yml"));
            }
            Assert.Equal("keep", await File.ReadAllTextAsync("output/notes.txt", TestContext.Current.CancellationToken));
            Assert.NotEqual("stale", await File.ReadAllTextAsync("output/first-openapi.yml", TestContext.Current.CancellationToken));
        }
        finally
        {
            Directory.SetCurrentDirectory(previousDirectory);
            Environment.SetEnvironmentVariable("KIOTA_UPDATE__DISABLED", previousUpdateSetting);
            directory.Delete(true);
        }
    }
}
