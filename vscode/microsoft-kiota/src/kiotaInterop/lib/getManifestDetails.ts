import * as rpc from "vscode-jsonrpc/node";

import { KiotaGetManifestDetailsConfiguration, KiotaManifestResult } from "..";
import connectToKiota from "../connect";

interface ManifestOptions {
  manifestPath: string;
  clearCache: boolean;
  apiIdentifier?: string;
}

export async function getManifestDetails({ manifestPath, clearCache, apiIdentifier }: ManifestOptions): Promise<KiotaManifestResult | undefined> {
  return connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaGetManifestDetailsConfiguration, KiotaManifestResult, void>('GetManifestDetails');

    const result = await connection.sendRequest(
      request,
      {
        manifestPath,
        apiIdentifier: apiIdentifier ?? '',
        clearCache
      }
    );
    return result;
  });
};