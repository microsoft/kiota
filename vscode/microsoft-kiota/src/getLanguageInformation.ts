import { connectToKiota, KiotaGenerationLanguage, LanguagesInformation } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function getLanguageInformation(context: vscode.ExtensionContext, language?: KiotaGenerationLanguage, descriptionUrl?: string): Promise<LanguagesInformation | undefined> {
    if(language && descriptionUrl) {
      return connectToKiota<LanguagesInformation>(context, async (connection) => {
        const request = new rpc.RequestType2<KiotaGenerationLanguage, string, LanguagesInformation, void>(
            "Info"
        );
        return await connection.sendRequest(
            request,
            language,
            descriptionUrl
        );
      });
    } else {
      return connectToKiota<LanguagesInformation>(context, async (connection) => {
        const request = new rpc.RequestType0<LanguagesInformation, void>(
            "Info"
        );
        return await connection.sendRequest(
            request
        );
      });
    }
};