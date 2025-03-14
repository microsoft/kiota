import { KiotaLogEntry, updateClients } from '..';
import { setupKiotaStubs } from './stubs.util';

describe("update Clients", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should return results when successful', async () => {
    const mockResults: KiotaLogEntry[] = [
      {
        level: 0,
        message: 'updated successfully'
      }
    ];

    connectionStub.mockResolvedValue(mockResults);
    const results = await updateClients({ clearCache: false, cleanOutput: false, workspacePath: 'test' });
    expect(results).toBeDefined();
  });

});

