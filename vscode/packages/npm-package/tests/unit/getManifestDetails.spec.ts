import { KiotaManifestResult } from '../../types';
import { getManifestDetails } from '../../lib/getManifestDetails';
import { setupKiotaStubs } from './stubs.util';

describe("get manifest details", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should return path details when successful', async () => {
    const mockResults: KiotaManifestResult = {
      logs: []
    };

    connectionStub.mockResolvedValue(mockResults);
    const details = await getManifestDetails({ manifestPath: '', clearCache: true, apiIdentifier: 'my-api' });
    expect(details).toBeDefined();
  });

});