import * as rpc from "vscode-jsonrpc/node";

import { ValidateOpenApiResult as ValidateOpenApiResult } from "..";
import connectToKiota from '../connect';

interface ValidateOpenApiRequest {
  descriptionPath: string;
}

/**
 * Validates an OpenAPI manifest by connecting to the Kiota service.
 *
 * @param {ValidateOpenApiRequest} request The request object.
 * @param {string} request.descriptionPath The path to the manifest file.
 * @returns {Promise<ValidateOpenApiResult | undefined>} A promise that resolves to the result of the validation. 
 * @throws {Error} If an error occurs while connecting to the Kiota service or validating the manifest.
 */
export async function validateOpenApi({ descriptionPath }: ValidateOpenApiRequest): Promise<ValidateOpenApiResult | undefined> {
  const result = await connectToKiota(async (connection) => {
    const request = new rpc.RequestType<ValidateOpenApiRequest, ValidateOpenApiResult, void>('ValidateManifest');

    return await connection.sendRequest(
      request,
      {
        descriptionPath
      }
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};
