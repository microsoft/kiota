import * as rpc from "vscode-jsonrpc/node";

import { KiotaShowConfiguration, KiotaShowResult } from ".";
import connectToKiota from "./connect";

interface KiotaResultOptions {
  includeFilters: string[];
  descriptionPath: string;
  excludeFilters: string[];
  clearCache: boolean;
}

export async function showKiotaResult({ includeFilters, descriptionPath, excludeFilters, clearCache }: KiotaResultOptions): Promise<KiotaShowResult | undefined> {
  return connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaShowConfiguration, KiotaShowResult, void>('Show');

    const result = await connection.sendRequest(request, {
      includeFilters,
      excludeFilters,
      descriptionPath,
      clearCache
    });
    return result;
  });
};