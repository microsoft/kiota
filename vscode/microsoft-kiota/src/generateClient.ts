import { connectToKiota, KiotaGenerationLanguage, KiotaLogEntry } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function generateClient(context: vscode.ExtensionContext, descriptionPath: string, output: string, language: KiotaGenerationLanguage, includeFilters: string[], excludeFilters: string[], clientClassName: string, clientNamespaceName: string): Promise<KiotaLogEntry[] | undefined> {
    return connectToKiota<KiotaLogEntry[]>(context, async (connection) => {
      const request = new rpc.RequestType7<string, string, KiotaGenerationLanguage, string[], string[], string, string, KiotaLogEntry[], void>(
        "Generate"
      );
      return await connection.sendRequest(
        request,
        descriptionPath,
        output,
        language,
        includeFilters,
        excludeFilters,
        clientClassName,
        clientNamespaceName
      );
    });
};