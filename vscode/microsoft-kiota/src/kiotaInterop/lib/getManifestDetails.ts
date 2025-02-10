import * as rpc from "vscode-jsonrpc/node";

import { KiotaGetManifestDetailsConfiguration, KiotaManifestResult } from "..";
import connectToKiota from "../connect";

interface ManifestOptions {
  manifestPath: string;
  clearCache: boolean;
  apiIdentifier?: string;
}

export async function getManifestDetails({ manifestPath, clearCache, apiIdentifier }: ManifestOptions): Promise<KiotaManifestResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<KiotaGetManifestDetailsConfiguration, KiotaManifestResult, void>('GetManifestDetails');

    return await connection.sendRequest(
      request,
      {
        manifestPath,
        apiIdentifier: apiIdentifier ?? '',
        clearCache
      }
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};