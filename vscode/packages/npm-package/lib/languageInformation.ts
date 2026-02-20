import * as rpc from "vscode-jsonrpc/node";

import { LanguagesInformation } from "../index.js";
import connectToKiota from "../connect.js";

export interface LanguageInformationConfiguration {
  descriptionUrl: string;
  clearCache: boolean;
}

/**
 * Retrieves language information by connecting to Kiota.
 *
 * This function establishes a connection to Kiota and sends a request to retrieve
 * language information. If the request is successful, it returns the language information.
 * If an error occurs during the request, the error is thrown.
 *
 * @returns {Promise<LanguagesInformation | undefined>} A promise that resolves to the language information or undefined if an error occurs.
 * @throws {Error} Throws an error if the request fails.
 */
export async function getLanguageInformationInternal(): Promise<
  LanguagesInformation | undefined
> {
  const result = await connectToKiota<LanguagesInformation>(
    async (connection) => {
      const request = new rpc.RequestType0<LanguagesInformation, void>("Info");
      return await connection.sendRequest(request);
    },
  );

  if (result instanceof Error) {
    throw result;
  }

  return result;
}

/**
 * Retrieves language information based on the provided description URL.
 *
 * @param {LanguageInformationConfiguration} config - The configuration object containing the description URL and cache clearing option.
 * @param {string} config.descriptionUrl - The URL of the description to retrieve language information for.
 * @param {boolean} config.clearCache - A flag indicating whether to clear the cache before retrieving the information.
 *
 * @returns {Promise<LanguagesInformation | undefined>} A promise that resolves to the language information or undefined if an error occurs.
 *
 * @throws {Error} Throws an error if the request fails.
 */
export async function getLanguageInformationForDescription({
  descriptionUrl,
  clearCache,
}: LanguageInformationConfiguration): Promise<
  LanguagesInformation | undefined
> {
  const result = await connectToKiota<LanguagesInformation>(
    async (connection) => {
      const request = new rpc.RequestType2<
        string,
        boolean,
        LanguagesInformation,
        void
      >("InfoForDescription");
      return await connection.sendRequest(request, descriptionUrl, clearCache);
    },
  );

  if (result instanceof Error) {
    throw result;
  }

  return result;
}
