import { connectToKiota, LanguagesInformation } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

let _languageInformation: LanguagesInformation | undefined; // doesn't change over the lifecycle of the extension
export async function getLanguageInformation(context: vscode.ExtensionContext): Promise<LanguagesInformation | undefined> {
    if(_languageInformation)
    {
        return _languageInformation;
    } 
    const result = await getLanguageInformationInternal(context);
    if (result) {
      _languageInformation = result;
    }
    return result;
};

function getLanguageInformationInternal(context: vscode.ExtensionContext): Promise<LanguagesInformation | undefined> {
  return connectToKiota<LanguagesInformation>(context, async (connection) => {
    const request = new rpc.RequestType0<LanguagesInformation, void>(
        "Info"
    );
    return await connection.sendRequest(
        request,
    );
  });
};

export function getLanguageInformationForDescription(context: vscode.ExtensionContext, descriptionUrl: string): Promise<LanguagesInformation | undefined> {
  return connectToKiota<LanguagesInformation>(context, async (connection) => {
    const request = new rpc.RequestType1<string, LanguagesInformation, void>(
        "InfoForDescription"
    );
    return await connection.sendRequest(
        request,
        descriptionUrl
    );
  });
};