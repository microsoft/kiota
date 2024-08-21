import { QuickPickItem } from "vscode";

export enum GenerationType {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Client = 0,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    Plugin = 1,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    ApiManifest = 2,
};

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
};

export enum KiotaPluginType {
    // eslint-disable-next-line @typescript-eslint/naming-convention
    OpenAI = 0,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    ApiManifest = 1,
    // eslint-disable-next-line @typescript-eslint/naming-convention
    ApiPlugin = 2,
};  

export function parseGenerationType(generationType: string | QuickPickItem | undefined): GenerationType {
    if(typeof generationType !== 'string') {
        throw new Error('generationType has not been selected yet');
    }
    switch(generationType) {
        case "client":
            return GenerationType.Client;
        case "plugin":
            return GenerationType.Plugin;
        case "apimanifest":
            return GenerationType.ApiManifest;
        default:
            throw new Error(`Unknown generation type ${generationType}`);
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
        case "CLI":
            return KiotaGenerationLanguage.CLI;
        default:
            throw new Error("unknown language");
    }
}

export function parsePluginType(values: string[]): KiotaPluginType[] {
    return values.map(value => {
        switch (value.toLowerCase()) {
            case "openai":
                return KiotaPluginType.OpenAI;
            case "apimanifest":
                return KiotaPluginType.ApiManifest;
            case "apiplugin":
                return KiotaPluginType.ApiPlugin;
            default:
                throw new Error(`unknown plugin type: ${value}`);
        }
    });
}
