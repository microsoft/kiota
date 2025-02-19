export enum KiotaGenerationLanguage {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  CSharp = 0,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Java = 1,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  TypeScript = 2,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  PHP = 3,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Python = 4,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Go = 5,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Swift = 6,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Ruby = 7,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  CLI = 8,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Dart = 9,
};

export enum KiotaPluginType {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  OpenAI = 0,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  ApiManifest = 1,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  ApiPlugin = 2,
};

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
export interface KiotaTreeResult extends KiotaLoggedResult {
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
    case KiotaGenerationLanguage.Dart:
      return "Dart";
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
  KiotaGenerationLanguage.Dart,
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
  // eslint-disable-next-line @typescript-eslint/naming-convention
  DependencyType: DependencyType;
}
export enum MaturityLevel {
  experimental = 0,
  preview = 1,
  stable = 2,
}

export enum DependencyType {
  abstractions,
  serialization,
  authentication,
  http,
  bundle,
  additional,
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
  pluginAuthRefid?: string;
  pluginAuthType?: PluginAuthType | null;
}

export enum PluginAuthType {
  oAuthPluginVault = "OAuthPluginVault",
  apiKeyPluginVault = "ApiKeyPluginVault"
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
  authType?: PluginAuthType,
  authReferenceId?: string;
}

export type ClientOrPluginProperties = ClientObjectProperties | PluginObjectProperties;

export interface LanguagesInformation {
  [key: string]: LanguageInformation;
}

export interface KiotaResult extends KiotaLoggedResult {
  isSuccess: boolean;
}
