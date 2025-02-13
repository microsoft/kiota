import { KiotaShowResult, showKiotaResult } from '..';
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


  test('should return path details when successful', async () => {
    const mockResults: KiotaShowResult = {
      apiTitle: 'my-api',
      rootNode: {
        segment: "segment",
        path: "path",
        children: [
          {
            segment: "segment",
            path: "path",
            children: [],
            selected: true,
            isOperation: true,
            documentationUrl: "http://example.com",
            clientNameOrPluginName: "clientNameOrPluginName"
          }
        ]
      },
      logs: []
    };

    connectionStub.mockResolvedValue(mockResults);
    const version = await showKiotaResult({ includeFilters: [], descriptionPath: 'descriptionPath', excludeFilters: [], clearCache: false });
    expect(version).toBeDefined();
  });

});