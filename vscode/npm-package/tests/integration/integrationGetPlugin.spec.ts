import { getPluginManifest } from '../../lib/getPluginManifest';

describe("getPlugin", () => {
  test('getPlugin_Valid', async () => {
    const pluginManifestPath = '../../tests/Kiota.Builder.IntegrationTests/PluginDiscriminatorSample.json';
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: pluginManifestPath
    });
    expect(actualPluginManifest).toBeDefined();
  });

  test('getPlugin_WithSecurity', async () => {
    const pluginManifestPath = '../../tests/Kiota.Builder.IntegrationTests/PluginModelWithSecurity.json';
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: pluginManifestPath
    });
    expect(actualPluginManifest).toBeDefined();
  });

});