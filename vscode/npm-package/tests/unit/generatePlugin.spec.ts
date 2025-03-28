import { generatePlugin } from '../../lib/generatePlugin';
import { KiotaLogEntry } from '../../types';
import { KiotaPluginType } from '../../types';
import { setupKiotaStubs } from './stubs.util';

describe("generate plugin", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should generate a plugin when successful', async () => {
    const mockResults: KiotaLogEntry[] = [
      {
        level: 0,
        message: "Generation complete"
      }
    ];
    connectionStub.mockResolvedValue(mockResults);

    const results = await generatePlugin({
      descriptionPath: 'openAPIFilePath',
      outputPath: 'outputPath',
      includePatterns: [],
      excludePatterns: [],
      pluginType: KiotaPluginType.ApiPlugin,
      pluginName: 'ApiClient',
      clearCache: false,
      cleanOutput: false,
      disabledValidationRules: [],
      operation: 0,
      workingDirectory: ''
    });

    expect(results).toBeDefined();
  });
});