import { generatePlugin } from '../lib/generatePlugin';
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
      pluginType: pluginType,
      pluginName: 'test3',
      operation: ConsumerOperation.Generate,
      workingDirectory: ''
    });
    expect(actual).toBeDefined();
  });

});