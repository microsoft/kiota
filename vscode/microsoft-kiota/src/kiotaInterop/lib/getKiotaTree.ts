import * as rpc from "vscode-jsonrpc/node";

import { KiotaShowConfiguration, KiotaTreeResult } from "..";
import connectToKiota from "../connect";

interface KiotaResultOptions {
  includeFilters: string[];
  descriptionPath: string;
  excludeFilters: string[];
  clearCache: boolean;
}

export async function getKiotaTree({ includeFilters, descriptionPath, excludeFilters, clearCache }: KiotaResultOptions): Promise<KiotaTreeResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaShowConfiguration, KiotaTreeResult, void>('Show');

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