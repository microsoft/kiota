import * as sinon from 'sinon';

import { KiotaPluginType } from '../../types/enums';
import { generatePlugin } from '../generatePlugin';
import { setupKiotaStubs } from './stubs.util';

describe("generate plugin", () => {
  let connectionStub: sinon.SinonStub;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    sinon.restore();
  });


  test('should generate a plugin when successful', async () => {
    const mockResults = [
      {
        level: 0,
        message: "Generation complete"
      }
    ]; connectionStub.resolves(mockResults);

    const results = await generatePlugin({
      openAPIFilePath: 'openAPIFilePath',
      outputPath: 'outputPath',
      includePatterns: [],
      excludePatterns: [],
      pluginTypes: [KiotaPluginType.ApiPlugin],
      clientClassName: 'ApiClient',
      clearCache: false,
      cleanOutput: false,
      disabledValidationRules: [],
      operation: 0,
      workingDirectory: ''
    });

    expect(results).toBeDefined();
  });
});