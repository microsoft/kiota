import * as sinon from 'sinon';

import { KiotaGenerationLanguage } from '../../types/enums';
import { generateClient } from '../generateClient';
import { setupKiotaStubs } from './stubs.util';

describe("generate client", () => {
  let connectionStub: sinon.SinonStub;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    sinon.restore();
  });


  test('should generate a client when successful', async () => {
    const mockResults = [
      {
        level: 0,
        message: "Generation complete"
      }
    ]; connectionStub.resolves(mockResults);

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