import { connectToKiota, KiotaGenerationLanguage, LanguagesInformation } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";

export function getLanguageInformation(language: KiotaGenerationLanguage, descriptionUrl: string): Promise<LanguagesInformation | undefined> {
    return connectToKiota<LanguagesInformation>(async (connection) => {
      const request = new rpc.RequestType2<KiotaGenerationLanguage, string, LanguagesInformation, void>(
        "Info"
      );
      return await connection.sendRequest(
        request,
        language,
        descriptionUrl
      );
  });
};