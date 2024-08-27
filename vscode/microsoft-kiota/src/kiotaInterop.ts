import * as cp from 'child_process';
import * as vscode from "vscode";
import * as rpc from 'vscode-jsonrpc/node';
import { KiotaGenerationLanguage, KiotaPluginType } from './enums';
import { ensureKiotaIsPresent, getKiotaPath } from './kiotaInstall';
import { getWorkspaceJsonDirectory } from "./util";

export async function connectToKiota<T>(context: vscode.ExtensionContext, callback:(connection: rpc.MessageConnection) => Promise<T | undefined>, workingDirectory:string = getWorkspaceJsonDirectory()): Promise<T | undefined> {
  const kiotaPath = getKiotaPath(context);
  await ensureKiotaIsPresent(context);
  const childProcess = cp.spawn(kiotaPath, ["rpc"],{
    cwd: workingDirectory,
    env: {
        ...process.env,
        // eslint-disable-next-line @typescript-eslint/naming-convention
        KIOTA_CONFIG_PREVIEW: "true",
    }
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
    selected?: boolean,
    isOperation?: boolean;
    documentationUrl?: string;
    clientNameOrPluginName?: string;
}
interface CacheClearableConfiguration {
    clearCache: boolean;
}

export interface KiotaShowConfiguration extends CacheClearableConfiguration {
    includeFilters: string[];
    excludeFilters: string[];
    descriptionPath: string;
}
export interface KiotaGetManifestDetailsConfiguration extends CacheClearableConfiguration {
    manifestPath: string;
    apiIdentifier: string;
}

interface KiotaLoggedResult {
    logs: KiotaLogEntry[];
}
export interface KiotaShowResult extends KiotaLoggedResult {
    rootNode?: KiotaOpenApiNode;
    apiTitle?: string;
}

export interface KiotaManifestResult extends KiotaLoggedResult {
    apiDescriptionPath?: string;
    selectedPaths?: string[];
}

export interface KiotaSearchResult extends KiotaLoggedResult {
    results: Record<string, KiotaSearchResultItem>;
}
export interface KiotaSearchResultItem {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Title: string;
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Description: string;
  // eslint-disable-next-line @typescript-eslint/naming-convention
  ServiceUrl?: string;
  // eslint-disable-next-line @typescript-eslint/naming-convention
  DescriptionUrl?: string;
  // eslint-disable-next-line @typescript-eslint/naming-convention
  VersionLabels?: string[];
}

export enum ConsumerOperation {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Add,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Edit,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Remove,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Generate
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
        case KiotaGenerationLanguage.CLI:
            return "CLI";
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
    KiotaGenerationLanguage.CLI,
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
    // eslint-disable-next-line @typescript-eslint/naming-convention
    MaturityLevel: MaturityLevel;
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Dependencies: LanguageDependency[];
    // eslint-disable-next-line @typescript-eslint/naming-convention
    DependencyInstallCommand: string;
    // eslint-disable-next-line @typescript-eslint/naming-convention
    ClientNamespaceName: string;
    // eslint-disable-next-line @typescript-eslint/naming-convention
    ClientClassName: string;
    // eslint-disable-next-line @typescript-eslint/naming-convention
    StructuredMimeTypes: string[];
}
export interface LanguageDependency {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Name: string;
    // eslint-disable-next-line @typescript-eslint/naming-convention
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
export interface ConfigurationFile {
    version: string;
    clients: Record<string, ClientObjectProperties>;
    plugins: Record<string, PluginObjectProperties>;
}

export interface GenerationConfiguration {
    cleanOutput: boolean;
    clearCache: boolean;
    clientClassName: string;
    clientNamespaceName: string;
    deserializers: string[];
    disabledValidationRules: string[];
    excludeBackwardCompatible: boolean;
    excludePatterns: string[];
    includeAdditionalData: boolean;
    includePatterns: string[];
    language: KiotaGenerationLanguage;
    openAPIFilePath: string;
    outputPath: string;
    serializers: string[];
    structuredMimeTypes: string[];
    usesBackingStore: boolean;
    pluginTypes: KiotaPluginType[];
    operation: ConsumerOperation;
}

interface WorkspaceObjectProperties {
    descriptionLocation: string;
    includePatterns: string[];
    excludePatterns: string[];
    outputPath: string;
}

export interface ClientObjectProperties extends WorkspaceObjectProperties {
    language: string;
    structuredMimeTypes: string[];
    clientNamespaceName: string;
    usesBackingStore: boolean;
    includeAdditionalData: boolean;
    excludeBackwardCompatible: boolean;
    disabledValidationRules: string[];
}

export interface PluginObjectProperties extends WorkspaceObjectProperties {
    types: string[];
}

export type ClientOrPluginProperties = ClientObjectProperties | PluginObjectProperties;
