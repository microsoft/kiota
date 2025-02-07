import * as sinon from 'sinon';

import { migrateFromLockFile } from '../migrateFromLockFile';
import { setupKiotaStubs } from './stubs.util';

describe("migrate from lock file", () => {
  let connectionStub: sinon.SinonStub;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    sinon.restore();
  });

  test('should return success when successful', async () => {
    const mockResults = [
      {
        level: 0,
        message: 'migrated successfully'
      }
    ];

    connectionStub.resolves(mockResults);
    const results = await migrateFromLockFile('lockfile');
    expect(results).toBeDefined();
  });

});