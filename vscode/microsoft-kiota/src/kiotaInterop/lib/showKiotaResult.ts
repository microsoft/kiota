import * as rpc from "vscode-jsonrpc/node";

import { KiotaShowConfiguration, KiotaShowResult } from "..";
import connectToKiota from "../connect";

interface KiotaResultOptions {
  includeFilters: string[];
  descriptionPath: string;
  excludeFilters: string[];
  clearCache: boolean;
}

export async function showKiotaResult({ includeFilters, descriptionPath, excludeFilters, clearCache }: KiotaResultOptions): Promise<KiotaShowResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaShowConfiguration, KiotaShowResult, void>('Show');

    return await connection.sendRequest(request, {
      includeFilters,
      excludeFilters,
      descriptionPath,
      clearCache
    });
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};