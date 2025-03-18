import { KiotaSearchResultItem, searchDescription } from '..';
import { setupKiotaStubs } from './stubs.util';

describe("search description", () => {
  let connectionStub: jest.Mock;

  beforeEach(() => {
    const stubs = setupKiotaStubs();
    connectionStub = stubs.connectionStub;
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });


  test('should return search results when search is successful', async () => {
    const mockResults: Record<string, KiotaSearchResultItem> = {
      'results': {
        Title: 'Item 1',
        DescriptionUrl: 'http://example.com',
        Description: 'Description for Item 1'
      },
    };

    connectionStub.mockResolvedValue(mockResults);
    const result = await searchDescription({ searchTerm: 'test', clearCache: false });
    expect(result).toEqual(mockResults.results);
  });

  test('should return undefined when no results are found', async () => {
    connectionStub.mockResolvedValue(undefined);
    const result = await searchDescription({ searchTerm: 'test', clearCache: false });
    expect(result).toBeUndefined();
  });

  test('should throw error when search fails', async () => {
    connectionStub.mockRejectedValue(new Error('Search failed'));
    try {
      await searchDescription({ searchTerm: 'test', clearCache: false });
    } catch (error) {
      expect(`[${error}]`).toEqual('[Error: Search failed]');
    }
  });
});