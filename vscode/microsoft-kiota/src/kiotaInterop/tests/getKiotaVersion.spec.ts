import * as sinon from 'sinon';

import { getKiotaVersion } from '..';
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


  test('should return version when successful', async () => {
    const mockResults: string = '1.0.0';

    connectionStub.resolves(mockResults);
    const version = await getKiotaVersion();
    expect(version).toBeDefined();
  });

  test('should throw error when ensureKiotaIsPresent fails', async () => {
    connectionStub.rejects(new Error('Installation failed'));
    try {
      await getKiotaVersion();
    } catch (error) {
      expect(`[${error}]`).toEqual("[Error: Installation failed]");
    }
  });
});