import * as rpc from "vscode-jsonrpc/node";

import { LanguagesInformation } from "..";
import connectToKiota from "../connect";

interface LanguageInformationConfiguration {
  descriptionUrl: string; clearCache: boolean;
}

export function getLanguageInformationInternal(): Promise<LanguagesInformation | undefined> {
  return connectToKiota<LanguagesInformation>(async (connection) => {
    const request = new rpc.RequestType0<LanguagesInformation, void>(
      "Info"
    );
    return await connection.sendRequest(
      request,
    );
  });
};

export function getLanguageInformationForDescription({ descriptionUrl, clearCache }: LanguageInformationConfiguration):
  Promise<LanguagesInformation | undefined> {
  return connectToKiota<LanguagesInformation>(async (connection) => {
    const request = new rpc.RequestType2<string, boolean, LanguagesInformation, void>(
      "InfoForDescription"
    );
    return await connection.sendRequest(
      request,
      descriptionUrl,
      clearCache
    );
  });
};