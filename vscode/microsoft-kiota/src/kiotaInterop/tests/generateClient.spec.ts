import { generateClient, KiotaLogEntry } from '..';
import { KiotaGenerationLanguage } from '../types';
import { setupKiotaStubs } from './stubs.util';

describe("generate client", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should generate a client when successful', async () => {
    const mockResults: KiotaLogEntry[] = [
      {
        level: 0,
        message: "Generation complete"
      }
    ]; connectionStub.mockResolvedValue(mockResults);

    const results = await generateClient({
      openAPIFilePath: 'openAPIFilePath',
      outputPath: 'outputPath',
      language: KiotaGenerationLanguage.CSharp,
      includePatterns: [],
      excludePatterns: [],
      clientClassName: 'ApiClient',
      clientNamespaceName: 'ApiSdk',
      usesBackingStore: false,
      clearCache: false,
      cleanOutput: false,
      excludeBackwardCompatible: false,
      disabledValidationRules: [],
      serializers: [],
      deserializers: [],
      structuredMimeTypes: [],
      includeAdditionalData: false,
      operation: 0,
      workingDirectory: ''
    });

    expect(results).toBeDefined();
  });
});