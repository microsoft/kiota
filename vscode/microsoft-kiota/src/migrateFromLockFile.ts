import { connectToKiota, KiotaLogEntry } from "./kiotaInterop";
import * as rpc from "vscode-jsonrpc/node";
import * as vscode from "vscode";

export function migrateFromLockFile(context: vscode.ExtensionContext, lockFileDirectory: string): Promise<KiotaLogEntry[] | undefined> {
    return connectToKiota(context, async (connection) => {
        const request = new rpc.RequestType1<string, KiotaLogEntry[], void>(
            "MigrateFromLockFile"
        );
        const result = await connection.sendRequest(
            request,
            lockFileDirectory
        );
        return result;
    });
};