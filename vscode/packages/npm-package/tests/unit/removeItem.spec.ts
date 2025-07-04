import { KiotaLogEntry, KiotaResult } from '../../types';
import { removeClient, removePlugin } from '../../lib/removeItem';
import { setupKiotaStubs } from './stubs.util';

describe("remove Client", () => {
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
        message: 'removed successfully'
      }
    ];

    connectionStub.mockResolvedValue(mockResults);
    const results = await removeClient({ clientName: 'test', cleanOutput: false, workingDirectory: process.cwd() });
    expect(results).toBeDefined();
  });

});

describe("remove Plugin", () => {
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
        message: 'removed successfully'
      }
    ];

    connectionStub.mockResolvedValue(mockResults);
    const results: KiotaResult | undefined = await removePlugin({ pluginName: 'test', cleanOutput: false, workingDirectory: process.cwd() })!;
    expect(results).toBeDefined();
    expect(results?.isSuccess).toBeTruthy();

  });

});