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
