import { generatePlugin } from '../lib/generatePlugin';
import { getKiotaTree } from '../lib/getKiotaTree';
import { getPluginManifest } from '../lib/getPluginManifest';
import { ConsumerOperation, KiotaPluginType } from '../types';

describe("getPlugin", () => {
  test('getPlugin_Valid', async () => {
    const pluginManifestPath = '../../tests/Kiota.Builder.IntegrationTests/PluginDiscriminatorSample.yaml';
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: pluginManifestPath
    });
    expect(actualPluginManifest).toBeDefined();
  });

  test('getPlugin_WithSecurity', async () => {
    const pluginManifestPath = '../../tests/Kiota.Builder.IntegrationTests/PluginModelWithSecurity.yaml';
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: pluginManifestPath
    });
    expect(actualPluginManifest).toBeDefined();
  });

});