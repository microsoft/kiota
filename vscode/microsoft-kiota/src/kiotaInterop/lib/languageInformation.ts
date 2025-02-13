import * as rpc from "vscode-jsonrpc/node";

import { LanguagesInformation } from "..";
import connectToKiota from "../connect";

interface LanguageInformationConfiguration {
  descriptionUrl: string; clearCache: boolean;
}

export async function getLanguageInformationInternal(): Promise<LanguagesInformation | undefined> {
  const result = await connectToKiota<LanguagesInformation>(async (connection) => {
    const request = new rpc.RequestType0<LanguagesInformation, void>(
      "Info"
    );
    return await connection.sendRequest(
      request,
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};

export async function getLanguageInformationForDescription({ descriptionUrl, clearCache }: LanguageInformationConfiguration):
  Promise<LanguagesInformation | undefined> {
  const result = await connectToKiota<LanguagesInformation>(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, LanguagesInformation, void>(
      "InfoForDescription"
    );
    return await connection.sendRequest(
      request,
      descriptionUrl,
      clearCache
    );
  });

  if (result instanceof Error) {
    throw result;
  }

  return result;
};