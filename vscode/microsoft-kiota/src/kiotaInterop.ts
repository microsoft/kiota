import * as vscode from "vscode";
import * as cp from 'child_process';
import * as rpc from 'vscode-jsonrpc/node';

export async function connectToKiota<T>(callback:(connection: rpc.MessageConnection) => Promise<T | undefined>): Promise<T | undefined> {
  const childProcess = cp.spawn("C:\\sources\\github\\kiota\\src\\kiota\\bin\\Debug\\net7.0\\kiota.exe", ["rpc"],{
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
  level: LogLevel;
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

interface KiotaLoggedResult {
    logs: KiotaLogEntry[];
}
export interface KiotaShowResult extends KiotaLoggedResult {
    rootNode?: KiotaOpenApiNode;
}

export interface KiotaSearchResult extends KiotaLoggedResult {
    results: Record<string, KiotaSearchResultItem>;
}
export interface KiotaSearchResultItem {
  Title: string;
  Description: string;
  ServiceUrl?: string;
  DescriptionUrl?: string;
  VersionLabels?: string[];
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
];

/**
 * The log level from Kiota
 * @see https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel?view=dotnet-plat-ext-7.0
 */
export enum LogLevel {
    trace = 0,
    debug = 1,
    information = 2,
    warning = 3,
    error = 4,
    critical = 5,
    none = 6,
}
export function getLogEntriesForLevel(logEntries: KiotaLogEntry[], ...levels: LogLevel[]): KiotaLogEntry[] {
    return logEntries.filter((entry) => levels.indexOf(entry.level) !== -1);
}

export interface LanguagesInformation {
    [key: string]: LanguageInformation;
}
export interface LanguageInformation {
    MaturityLevel: MaturityLevel;
    Dependencies: LanguageDependency[];
    DependencyInstallCommand: string;
    ClientNamespaceName: string;
    ClientClassName: string;
    StructuredMimeTypes: string[];
}
export interface LanguageDependency {
    Name: string;
    Version: string;
}
export enum MaturityLevel {
    experimental = 0,
    preview = 1,
    stable = 2,
}
export function maturityLevelToString(level: MaturityLevel): string {
    switch (level) {
        case MaturityLevel.experimental:
            return "experimental";
        case MaturityLevel.preview:
            return "preview";
        case MaturityLevel.stable:
            return "stable";
        default:
            throw new Error("unknown level");
    }
}

export interface LockFile {
    descriptionHash: string;
    descriptionLocation: string;
    language: string;
    lockFileVersion: string;
    kiotaVersion: string;
    clientClassName: string;
    clientNamespaceName: string;
    usesBackingStore: boolean;
    includeAdditionalData: boolean;
    serializers: string[];
    deserializers: string[];
    structuredMimeTypes: string[];
    includePatterns: string[];
    excludePatterns: string[];
    disabledValidationRules: string[];
}