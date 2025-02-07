import * as sinon from 'sinon';

import { getManifestDetails, KiotaManifestResult } from '..';
import { setupKiotaStubs } from './stubs.util';

describe("getKiotaVersion", () => {
  let connectionStub: sinon.SinonStub;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    sinon.restore();
  });


  test('should return path details when successful', async () => {
    const mockResults: KiotaManifestResult = {
      logs: []
    };

    connectionStub.resolves(mockResults);
    const version = await getManifestDetails({ manifestPath: '', clearCache: true, apiIdentifier: 'my-api' });
    expect(version).toBeDefined();
  });

});