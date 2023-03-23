import { connectToKiota, KiotaSearchResult, KiotaSearchResultItem } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function searchDescription(context: vscode.ExtensionContext, searchTerm: string): Promise<Record<string, KiotaSearchResultItem> | undefined> {
    return connectToKiota<Record<string, KiotaSearchResultItem>>(context, async (connection) => {
      const request = new rpc.RequestType<string, KiotaSearchResult, void>(
        "Search"
      );
      const result = await connection.sendRequest(
        request,
        searchTerm
      );
      if(result) {
        return result.results;
      }
      return undefined;
    });
};