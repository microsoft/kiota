import { KiotaLogEntry } from '../../types';
import { migrateFromLockFile } from '../../lib/migrateFromLockFile';
import { setupKiotaStubs } from './stubs.util';

describe("migrate from lock file", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  test('should return success when successful', async () => {
    const mockResults: KiotaLogEntry[] = [
      {
        level: 0,
        message: 'migrated successfully'
      }
    ];

    connectionStub.mockResolvedValue(mockResults);
    const results = await migrateFromLockFile('lockfile');
    expect(results).toBeDefined();
  });

});