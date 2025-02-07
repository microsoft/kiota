import * as rpc from "vscode-jsonrpc/node";

import { KiotaSearchResult, KiotaSearchResultItem } from "..";
import connectToKiota from '../connect';
export function searchDescription(searchTerm: string, clearCache: boolean): Promise<Record<string, KiotaSearchResultItem> | undefined> {
  return connectToKiota<Record<string, KiotaSearchResultItem>>(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, KiotaSearchResult, void>(
      "Search"
    );
    const result = await connection.sendRequest(
      request,
      searchTerm,
      clearCache,
    );
    if (result) {
      return result.results;
    }
    return undefined;
  });
};