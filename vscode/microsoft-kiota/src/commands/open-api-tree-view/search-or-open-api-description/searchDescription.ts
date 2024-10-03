import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

import { KiotaSearchResultItem, connectToKiota, KiotaSearchResult } from "../../../kiotaInterop";

export function searchDescription(context: vscode.ExtensionContext, searchTerm: string, clearCache: boolean): Promise<Record<string, KiotaSearchResultItem> | undefined> {
  return connectToKiota<Record<string, KiotaSearchResultItem>>(context, async (connection) => {
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