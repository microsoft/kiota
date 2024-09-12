import { connectToKiota, LanguagesInformation } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

let _languageInformation: LanguagesInformation | undefined; // doesn't change over the lifecycle of the extension
export async function getLanguageInformation(context: vscode.ExtensionContext): Promise<LanguagesInformation | undefined> {
  if (_languageInformation) {
    return _languageInformation;
  }
  const result = await getLanguageInformationInternal(context);
  if (result) {
    _languageInformation = result;
  }
  return result;
};

function getLanguageInformationInternal(context: vscode.ExtensionContext): Promise<LanguagesInformation | undefined> {
  try {
    return connectToKiota<LanguagesInformation>(context, async (connection) => {
      const request = new rpc.RequestType0<LanguagesInformation, void>(
        "Info"
      );
      return await connection.sendRequest(
        request,
      );
    });
  } catch (error) {
    return Promise.resolve(undefined);
  }
};

export function getLanguageInformationForDescription(context: vscode.ExtensionContext, descriptionUrl: string, clearCache: boolean): Promise<LanguagesInformation | undefined> {
  try {
    return connectToKiota<LanguagesInformation>(context, async (connection) => {
      const request = new rpc.RequestType2<string, boolean, LanguagesInformation, void>(
        "InfoForDescription"
      );
      return await connection.sendRequest(
        request,
        descriptionUrl,
        clearCache
      );
    });
  } catch (error) {
    return Promise.resolve(undefined);
  }
};