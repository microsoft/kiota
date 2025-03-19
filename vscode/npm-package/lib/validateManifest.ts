import * as rpc from "vscode-jsonrpc/node";

import { ValidateManifestResult } from "..";
import connectToKiota from '../connect';

interface ValidateManifestRequest {
  manifestPath: string;
}

/**
 * Validates an OpenAPI manifest by connecting to the Kiota service.
 *
 * @param {ValidateManifestRequest} request The request object.
 * @param {string} request.manifestPath The path to the manifest file.
 * @returns {Promise<ValidateManifestResult | undefined>} A promise that resolves to the result of the validation. 
 * @throws {Error} If an error occurs while connecting to the Kiota service or validating the manifest.
 */
export async function validateManifest({ manifestPath }: ValidateManifestRequest): Promise<ValidateManifestResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<ValidateManifestRequest, ValidateManifestResult, void>('ValidateManifest');

    return await connection.sendRequest(
      request,
      {
        manifestPath
      }
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};
