import { KiotaTreeResult } from '../../types';
import { getKiotaTree } from '../../lib/getKiotaTree';
import { setupKiotaStubs } from './stubs.util';

describe("get kiota tree", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should return path details when successful', async () => {
    const mockResults: KiotaTreeResult = {
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
    const tree = await getKiotaTree({ includeFilters: [], descriptionPath: 'descriptionPath', excludeFilters: [], clearCache: false });
    expect(tree).toBeDefined();
  });

});