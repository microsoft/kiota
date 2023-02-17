import * as vscode from "vscode";
import * as cp from 'child_process';
import * as rpc from 'vscode-jsonrpc/node';

export async function connectToKiota<T>(callback:(connection: rpc.MessageConnection) => Promise<T | undefined>): Promise<T | undefined> {
  const childProcess = cp.spawn("C:\\sources\\github\\kiota\\src\\Kiota.JsonRpcServer\\bin\\Debug\\net7.0\\Kiota.JsonRpcServer.exe", ["stdio"],{
    cwd: vscode.workspace.workspaceFolders && vscode.workspace.workspaceFolders.length > 0 ? vscode.workspace.workspaceFolders[0].uri.fsPath : undefined
  });
  let connection = rpc.createMessageConnection(
    new rpc.StreamMessageReader(childProcess.stdout),
    new rpc.StreamMessageWriter(childProcess.stdin));
  connection.listen();
  try {
    return await callback(connection);
  } catch (error) {
    console.error(error);
    return undefined;
  } finally {
    connection.dispose();
    childProcess.kill();
  }
}

export interface KiotaLogEntry {
  level: number;
  message: string;
}

export interface KiotaOpenApiNode {
    segment: string,
    path: string,
    children: KiotaOpenApiNode[],
    selected: boolean,
}

export interface KiotaShowConfiguration {
    includeFilters: string[];
    excludeFilters: string[];
    descriptionPath: string;
}

export interface KiotaShowResult {
    logs: KiotaLogEntry[];
    rootNode?: KiotaOpenApiNode;
}

export enum KiotaGenerationLanguage {
    CSharp = 0,
    Java = 1,
    TypeScript = 2,
    PHP = 3,
    Python = 4,
    Go = 5,
    Swift = 6,
    Ruby = 7,
    Shell = 8,
}
export function generationLanguageToString(language: KiotaGenerationLanguage): string {
    switch (language) {
        case KiotaGenerationLanguage.CSharp:
            return "CSharp";
        case KiotaGenerationLanguage.Java:
            return "Java";
        case KiotaGenerationLanguage.TypeScript:
            return "TypeScript";
        case KiotaGenerationLanguage.PHP:
            return "PHP";
        case KiotaGenerationLanguage.Python:
            return "Python";
        case KiotaGenerationLanguage.Go:
            return "Go";
        case KiotaGenerationLanguage.Swift:
            return "Swift";
        case KiotaGenerationLanguage.Ruby:
            return "Ruby";
        case KiotaGenerationLanguage.Shell:
            return "Shell";
        default:
            throw new Error("unknown language");
    }
}
export function parseGenerationLanguage(value: string): KiotaGenerationLanguage {
    switch (value) {
        case "CSharp":
            return KiotaGenerationLanguage.CSharp;
        case "Java":
            return KiotaGenerationLanguage.Java;
        case "TypeScript":
            return KiotaGenerationLanguage.TypeScript;
        case "PHP":
            return KiotaGenerationLanguage.PHP;
        case "Python":
            return KiotaGenerationLanguage.Python;
        case "Go":
            return KiotaGenerationLanguage.Go;
        case "Swift":
            return KiotaGenerationLanguage.Swift;
        case "Ruby":
            return KiotaGenerationLanguage.Ruby;
        case "Shell":
            return KiotaGenerationLanguage.Shell;
        default:
            throw new Error("unknown language");
    }
}
export const allGenerationLanguages = [
    KiotaGenerationLanguage.CSharp,
    KiotaGenerationLanguage.Go,
    KiotaGenerationLanguage.Java,
    KiotaGenerationLanguage.PHP,
    KiotaGenerationLanguage.Python,
    KiotaGenerationLanguage.Ruby,
    KiotaGenerationLanguage.Shell,
    KiotaGenerationLanguage.Swift,
    KiotaGenerationLanguage.TypeScript,
]