import * as rpc from "vscode-jsonrpc/node";

import { KiotaSearchResult, KiotaSearchResultItem } from "..";
import connectToKiota from '../connect';

interface SearchConfiguration {
  searchTerm: string;
  clearCache: boolean;
}

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