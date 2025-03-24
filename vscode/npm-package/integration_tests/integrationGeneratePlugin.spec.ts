import { generatePlugin } from '../lib/generatePlugin';
import { getKiotaTree } from '../lib/getKiotaTree';
import { getPluginManifest } from '../lib/getPluginManifest';
import { ConsumerOperation, KiotaPluginType } from '../types';

describe("GeneratePlugin", () => {
  test('should create plugin manifest', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/DiscriminatorSample.yaml';
    const outputPath = './.tests_output';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginTypes: [pluginType],
      pluginName: 'test3',
      operation: ConsumerOperation.Generate,
      workingDirectory: ''
    });
    expect(actual).toBeDefined();

    if (!actual?.aiPlugin) {
      throw new Error('aiPlugin should be defined');
    }
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: actual?.aiPlugin
    });
    expect(actualPluginManifest).toBeDefined();

    if (!actual?.openAPISpec) {
      throw new Error('descriptionPath should be defined');
    }
    const actualApiManifest = await getKiotaTree({
      descriptionPath: actual?.openAPISpec,
    });
    expect(actualApiManifest).toBeDefined();
  });

  test('should create plugin manifest', async () => {
    const descriptionUrl = '../../tests/Kiota.Builder.IntegrationTests/ModelWithSecurity.yaml';
    const outputPath = './.tests_output';

    const pluginType = KiotaPluginType.ApiPlugin;
    const actual = await generatePlugin({
      descriptionPath: descriptionUrl,
      outputPath: outputPath,
      clearCache: false,
      pluginTypes: [pluginType],
      pluginName: 'withsecurity',
      operation: ConsumerOperation.Generate,
      workingDirectory: ''
    });
    expect(actual).toBeDefined();

    if (!actual?.aiPlugin) {
      throw new Error('aiPlugin should be defined');
    }
    const actualPluginManifest = await getPluginManifest({
      descriptionPath: actual?.aiPlugin
    });
    expect(actualPluginManifest).toBeDefined();

    if (!actual?.openAPISpec) {
      throw new Error('descriptionPath should be defined');
    }
    const actualApiManifest = await getKiotaTree({
      descriptionPath: actual?.openAPISpec,
    });
    expect(actualApiManifest).toBeDefined();
  });

});