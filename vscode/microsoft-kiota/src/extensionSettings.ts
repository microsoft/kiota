import * as vscode from "vscode";
import { KiotaGenerationLanguage } from "./kiotaInterop";

export function getExtensionSettings(extensionId: string) : ExtensionSettings {
    return {
        includeAdditionalData: getBooleanConfiguration(extensionId, "generate.includeAdditionalData.enabled"),
        backingStore: getBooleanConfiguration(extensionId, "generate.backingStore.enabled"),
        excludeBackwardCompatible: getBooleanConfiguration(extensionId, "generate.excludeBackwardCompatible.enabled"),
        cleanOutput: getBooleanConfiguration(extensionId, "cleanOutput.enabled"),
        clearCache: getBooleanConfiguration(extensionId, "clearCache.enabled"),
        disableValidationRules: getStringArrayConfiguration(extensionId, "generate.disableValidationRules"),
        structuredMimeTypes: getStringArrayConfiguration(extensionId, "generate.structuredMimeTypes"),
        languagesSerializationConfiguration: {
            [KiotaGenerationLanguage.CLI]: getLanguageSerializationConfiguration(extensionId, "CSharp"),
            [KiotaGenerationLanguage.CSharp]: getLanguageSerializationConfiguration(extensionId, "CSharp"),
            [KiotaGenerationLanguage.Go]: getLanguageSerializationConfiguration(extensionId, "Go"),
            [KiotaGenerationLanguage.Java]: getLanguageSerializationConfiguration(extensionId, "Java"),
            [KiotaGenerationLanguage.PHP]: getLanguageSerializationConfiguration(extensionId, "PHP"),
            [KiotaGenerationLanguage.Python]: getLanguageSerializationConfiguration(extensionId, "Python"),
            [KiotaGenerationLanguage.Ruby]: getLanguageSerializationConfiguration(extensionId, "Ruby"),
            [KiotaGenerationLanguage.Swift]: getLanguageSerializationConfiguration(extensionId, "Swift"),
            [KiotaGenerationLanguage.TypeScript]: getLanguageSerializationConfiguration(extensionId, "TypeScript"),
        },
    };
}
function getBooleanConfiguration(extensionId: string, configurationName: string): boolean {
    return vscode.workspace.getConfiguration(extensionId).get<boolean>(configurationName) ?? false;
}
function getStringArrayConfiguration(extensionId: string, configurationName: string): string[] {
    return vscode.workspace.getConfiguration(extensionId).get<string[]>(configurationName) ?? [];
}
function getLanguageSerializationConfiguration(extensionId: string, languageName: string): LanguageSerializationConfiguration {
    return {
        serializers: getStringArrayConfiguration(extensionId, `generate.serializer.${languageName}`),
        deserializers: getStringArrayConfiguration(extensionId, `generate.deserializer.${languageName}`),
    };
}

export interface ExtensionSettings {
    backingStore: boolean;
    excludeBackwardCompatible: boolean;
    cleanOutput: boolean;
    clearCache: boolean;
    disableValidationRules: string[];
    structuredMimeTypes: string[];
    includeAdditionalData: boolean;
    languagesSerializationConfiguration: Record<KiotaGenerationLanguage, LanguageSerializationConfiguration>;
}

interface LanguageSerializationConfiguration {
    serializers: string[];
    deserializers: string[];
}