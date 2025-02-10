import * as sinon from 'sinon';

import { getManifestDetails, KiotaManifestResult } from '..';
import { setupKiotaStubs } from './stubs.util';

describe("get manifest details", () => {
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
    const details = await getManifestDetails({ manifestPath: '', clearCache: true, apiIdentifier: 'my-api' });
    expect(details).toBeDefined();
  });

});