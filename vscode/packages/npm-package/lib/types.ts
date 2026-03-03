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
  Ruby = 6,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  Dart = 7,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  HTTP = 8,
}

export enum KiotaPluginType {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  OpenAI = 0,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  ApiManifest = 1,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  ApiPlugin = 2,
}

export interface KiotaLogEntry {
  level: LogLevel;
  message: string;
}

export enum OpenApiAuthType {
  None = 0,
  ApiKey = 1,
  Http = 2,
  OAuth2 = 3,
  OpenIdConnect = 4,
}

// key is the security scheme name, value is array of scopes
export interface SecurityRequirementObject {
  [name: string]: string[];
}

export interface KiotaOpenApiNode {
  segment: string;
  path: string;
  children: KiotaOpenApiNode[];
  operationId?: string;
  summary?: string;
  description?: string;
  selected?: boolean;
  isOperation?: boolean;
  documentationUrl?: string;
  clientNameOrPluginName?: string;
  authType?: OpenApiAuthType;
  logs?: KiotaLogEntry[];
  servers?: string[];
  security?: SecurityRequirementObject[];
  adaptiveCard?: AdaptiveCardInfo;
}

export interface AdaptiveCardInfo {
  dataPath: string;
  file: string;
}

export interface CacheClearableConfiguration {
  clearCache: boolean;
}

export interface KiotaShowConfiguration extends CacheClearableConfiguration {
  includeFilters: string[];
  excludeFilters: string[];
  descriptionPath: string;
  includeKiotaValidationRules: boolean;
}

export interface KiotaGetManifestDetailsConfiguration
  extends CacheClearableConfiguration {
  manifestPath: string;
  apiIdentifier: string;
}

export interface KiotaLoggedResult {
  logs: KiotaLogEntry[];
}

export enum OpenApiSpecVersion {
  // eslint-disable-next-line @typescript-eslint/naming-convention
  V2_0 = 0,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  V3_0 = 1,
  // eslint-disable-next-line @typescript-eslint/naming-convention
  V3_1 = 2,
}

export interface KiotaTreeResult extends KiotaLoggedResult {
  specVersion: OpenApiSpecVersion;
  rootNode?: KiotaOpenApiNode;
  apiTitle?: string;
  servers?: string[];
  security?: SecurityRequirementObject[];
  securitySchemes?: { [key: string]: SecuritySchemeObject };
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
  Generate,
}

export function generationLanguageToString(
  language: KiotaGenerationLanguage
): string {
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
    case KiotaGenerationLanguage.Ruby:
      return "Ruby";
    case KiotaGenerationLanguage.Dart:
      return "Dart";
    case KiotaGenerationLanguage.HTTP:
      return "HTTP";
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
  KiotaGenerationLanguage.TypeScript,
  KiotaGenerationLanguage.Dart,
  KiotaGenerationLanguage.HTTP,
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
  noWorkspace?: boolean;
  pluginAuthRefid?: string;
  pluginAuthType?: PluginAuthType | null;
}

export enum PluginAuthType {
  oAuthPluginVault = "OAuthPluginVault",
  apiKeyPluginVault = "ApiKeyPluginVault",
}

export interface WorkspaceObjectProperties {
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
  authType?: PluginAuthType;
  authReferenceId?: string;
}

export type ClientOrPluginProperties =
  | ClientObjectProperties
  | PluginObjectProperties;

export interface LanguagesInformation {
  [key: string]: LanguageInformation;
}

export interface KiotaResult extends KiotaLoggedResult {
  isSuccess: boolean;
}

export interface ValidateOpenApiResult extends KiotaLoggedResult {}

export interface GeneratePluginResult extends KiotaResult {
  aiPlugin: string;
  openAPISpec: string;
}

export interface PluginManifestResult extends KiotaResult {
  isValid: boolean;
  schema_version: string;
  name_for_human: string;
  functions: PluginFunction[];
  runtime: PluginRuntime[];
}

export interface PluginFunction {
  name: string;
  description: string;
}

export interface PluginAuth {
  type: string; // None, OAuthPluginVault, ApiKeyPluginVault
  reference_id?: string;
}

export interface PluginRuntime {
  type: string;
  auth: PluginAuth;
  run_for_functions: string[];
}

export type SecuritySchemeObject =
  | HttpSecurityScheme
  | ApiKeySecurityScheme
  | OAuth2SecurityScheme
  | OpenIdSecurityScheme;

export interface AuthReferenceId {
  referenceId: string;
}

export interface HttpSecurityScheme extends AuthReferenceId {
  type: "http";
  description?: string;
  scheme: string;
  bearerFormat?: string;
}

export interface ApiKeySecurityScheme extends AuthReferenceId {
  type: "apiKey";
  description?: string;
  name: string;
  in: string;
}

export interface OAuth2SecurityScheme extends AuthReferenceId {
  type: "oauth2";
  description?: string;
  flows: {
    implicit?: {
      authorizationUrl: string;
      refreshUrl?: string;
      scopes: { [scope: string]: string };
    };
    password?: {
      tokenUrl: string;
      refreshUrl?: string;
      scopes: { [scope: string]: string };
    };
    clientCredentials?: {
      tokenUrl: string;
      refreshUrl?: string;
      scopes: { [scope: string]: string };
    };
    authorizationCode?: {
      authorizationUrl: string;
      tokenUrl: string;
      refreshUrl?: string;
      scopes: { [scope: string]: string };
    };
  };
}

export interface OpenIdSecurityScheme extends AuthReferenceId {
  type: "openIdConnect";
  description?: string;
  openIdConnectUrl: string;
}
