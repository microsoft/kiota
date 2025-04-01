import * as rpc from "vscode-jsonrpc/node";

import { KiotaSearchResult, KiotaSearchResultItem } from "..";
import connectToKiota from '../connect';

export interface SearchConfiguration {
  searchTerm: string;
  clearCache: boolean;
}

/**
 * Searches for a description based on the provided search term and cache settings.
 *
 * @param {SearchConfiguration} config - The search configuration object.
 * @param {string} config.searchTerm - The term to search for.
 * @param {boolean} config.clearCache - Whether to clear the cache before searching.
 * @returns {Promise<Record<string, KiotaSearchResultItem> | undefined>} A promise that resolves to a record of search results or undefined if no results are found.
 * @throws {Error} Throws an error if the search operation fails.
 */
export async function searchDescription({ searchTerm, clearCache }: SearchConfiguration): Promise<Record<string, KiotaSearchResultItem> | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaSearchResult, void>(
      "Search"
    );
    return await connection.sendRequest(
      request,
      searchTerm,
      clearCache,
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  if (result) {
    return result.results;
  }

  return undefined;
};