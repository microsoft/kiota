import { getKiotaVersion } from '..';
import { setupKiotaStubs } from './stubs.util';

describe("getKiotaVersion", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should return version when successful', async () => {
    const mockResults: string = '1.0.0';

    connectionStub.mockResolvedValue(mockResults);
    const version = await getKiotaVersion();
    expect(version).toBeDefined();
  });

  test('should throw error when connection fails', async () => {
    connectionStub.mockRejectedValueOnce(new Error('Installation failed'));
    try {
      await getKiotaVersion();
    } catch (error) {
      expect(`[${error}]`).toEqual("[Error: Installation failed]");
    }
  });
});