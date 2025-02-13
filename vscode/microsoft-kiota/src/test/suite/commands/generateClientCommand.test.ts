// You can import and use all API from the 'vscode' module
// as well as import your extension to test it
import TelemetryReporter from "@vscode/extension-telemetry";
import * as path from "path";
import * as vscode from 'vscode';
import * as generateModule from "../../../commands/generate/generateClientCommand";
import * as languageInfoModule from "../../../commands/generate/generation-util";
import * as deepLinkParamsHandler from "../../../handlers/deepLinkParamsHandler";
import { KiotaGenerationLanguage, KiotaLogEntry } from "../../../kiotaInterop";
import * as generateStepsModule from "../../../modules/steps/generateSteps";
import * as dependenciesModule from "../../../providers/dependenciesViewProvider";
import * as treeModule from "../../../providers/openApiTreeProvider";
import * as settingsModule from "../../../types/extensionSettings";
import { WorkspaceGenerationContext } from "../../../types/WorkspaceGenerationContext";
import { getSanitizedString } from "../../../util";
import { IntegrationParams, transformToGenerationConfig } from "../../../utilities/deep-linking";
import * as msgUtilitiesModule from "../../../utilities/messaging";

let context: vscode.ExtensionContext = {
    subscriptions: [],
    workspaceState: {
        update: jest.fn().mockResolvedValue(undefined),
        keys: jest.fn().mockReturnValue([]),
        get: jest.fn().mockReturnValue(undefined)
    } as vscode.Memento,
    globalState: {} as any,
    secrets: {} as vscode.SecretStorage,
    extensionUri: vscode.Uri.parse(''),
    extensionPath: '',
    environmentVariableCollection: {} as vscode.GlobalEnvironmentVariableCollection,
    storagePath: '',
    globalStoragePath: '',
    logPath: '',
    languageModelAccessInformation:  {} as any,
    extensionMode: vscode.ExtensionMode.Test,
    asAbsolutePath: (relativePath: string) => relativePath,
    storageUri: vscode.Uri.parse(''),
    globalStorageUri: vscode.Uri.parse(''),
    logUri: vscode.Uri.parse(''),
    extension: {
        packageJSON: {
            telemetryInstrumentationKey: ""
        }
    } as vscode.Extension<any>
};

let extensionSettings = {
    includeAdditionalData:false,
    backingStore: false,
    excludeBackwardCompatible: false,
    cleanOutput: false,
    clearCache: true,
    disableValidationRules: [],
    structuredMimeTypes: [],
    languagesSerializationConfiguration: {
        [KiotaGenerationLanguage.CLI]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.CSharp]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.Go]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.Java]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.PHP]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.Python]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.Ruby]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.Swift]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.TypeScript]: { serializers: [], deserializers: [] },
        [KiotaGenerationLanguage.Dart]: { serializers: [], deserializers: [] }
    },
};

let result: KiotaLogEntry[] = [{level: 1, message: "Parsing OpenAPI file"},{level:2, message: "Generation completed successfully"}];

const setWorkspaceGenerationContext = (params: Partial<WorkspaceGenerationContext>):void =>{};

describe('GenerateClientCommand Test Suite', () => {
    let myOutputChannel: vscode.LogOutputChannel;

    beforeEach(() => {
        myOutputChannel = vscode.window.createOutputChannel("Kiota", {
            log: true,
        });
    });

    afterEach(() => {
        jest.clearAllMocks();
    });

    test('test function getName of GenerateClientCommand', () => {
        const treeProvider = jest.createMockFromModule<treeModule.OpenApiTreeProvider>("../../../providers/openApiTreeProvider");
        const viewProvider = jest.createMockFromModule<dependenciesModule.DependenciesViewProvider>("../../../providers/dependenciesViewProvider");
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        expect("kiota.openApiExplorer.generateClient").toEqual(generateClientCommand.getName());
    });

    test('test function execute of GenerateClientCommand with 0 selected paths', async () => {
        const treeProvider = jest.createMockFromModule<treeModule.OpenApiTreeProvider>("../../../providers/openApiTreeProvider");
        treeProvider.getSelectedPaths = jest.fn().mockReturnValue([]);
        const viewProvider = jest.createMockFromModule<dependenciesModule.DependenciesViewProvider>("../../../providers/dependenciesViewProvider");
        const vscodeWindowSpy = jest.spyOn(vscode.window, "showErrorMessage").mockResolvedValue(undefined);
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        await generateClientCommand.execute();
        expect((treeProvider.getSelectedPaths()).length).toEqual(0);
        expect(vscodeWindowSpy).toHaveBeenCalledWith(vscode.l10n.t("No endpoints selected, select endpoints first"));
    });

    test('test function execute of GenerateClientCommand with descriptionUrl unset', async () => {
        const treeProvider = jest.createMockFromModule<treeModule.OpenApiTreeProvider>("../../../providers/openApiTreeProvider");
        treeProvider.getSelectedPaths = jest.fn().mockReturnValue(["repairs"]);
        treeProvider.apiTitle = "Repairs OAD";
        const viewProvider = jest.createMockFromModule<dependenciesModule.DependenciesViewProvider>("../../../providers/dependenciesViewProvider");
        const vscodeWindowSpy = jest.spyOn(vscode.window, "showErrorMessage").mockResolvedValue(undefined);
        const getlanguageInfoFn = jest.spyOn(languageInfoModule, "getLanguageInformation").mockResolvedValue(undefined);
        const generateStepsFn = jest.spyOn(generateStepsModule, "generateSteps").mockResolvedValue({ generationType: "client" });
        const showUpgradeWarningMessageStub = jest.spyOn(msgUtilitiesModule, "showUpgradeWarningMessage").mockResolvedValue(undefined);
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        await generateClientCommand.execute();
        expect((treeProvider.getSelectedPaths()).length).toEqual(1);
        expect(vscodeWindowSpy).toHaveBeenCalledWith(vscode.l10n.t("No description found, select a description first"));
        expect(getlanguageInfoFn).toHaveBeenCalled();
        expect(generateStepsFn).toHaveBeenCalledWith(expect.objectContaining({
            clientClassName: treeProvider.clientClassName,
            clientNamespaceName: treeProvider.clientNamespaceName,
            language: treeProvider.language,
            outputPath: treeProvider.outputPath,
            pluginName: "RepairsOAD"
        }), undefined, {});
        expect(showUpgradeWarningMessageStub).toHaveBeenCalled();
    });

    test('test successful completion of function execute of GenerateClientCommand', async () => {
        const treeProvider = jest.createMockFromModule<treeModule.OpenApiTreeProvider>("../../../providers/openApiTreeProvider");
        Object.defineProperty(treeProvider, "descriptionUrl", {
            get: jest.fn().mockReturnValue("https://graph.microsoft.com/v1.0/$metadata")
        });
        treeProvider.getSelectedPaths = jest.fn().mockReturnValue(["repairs"]);
        treeProvider.apiTitle = "Repairs OAD";
        const viewProvider = jest.createMockFromModule<dependenciesModule.DependenciesViewProvider>("../../../providers/dependenciesViewProvider");
        const vscodeWindowSpy = jest.spyOn(vscode.window, "showErrorMessage").mockResolvedValue(undefined);
        const getlanguageInfoFn = jest.spyOn(languageInfoModule, "getLanguageInformation").mockResolvedValue(undefined);
        const showUpgradeWarningMessageStub = jest.spyOn(msgUtilitiesModule, "showUpgradeWarningMessage").mockResolvedValue(undefined);
        const getExtensionSettingsStub = jest.spyOn(settingsModule, "getExtensionSettings").mockReturnValue(extensionSettings);
        const generateStepsFn = jest.spyOn(generateStepsModule, "generateSteps").mockResolvedValue({
            generationType: "plugin",
            outputPath: "path/to/temp/folder",
            pluginName: "OverridingRepairsOADname"
        });
        deepLinkParamsHandler.setDeepLinkParams({
            name: "OverridingRepairsOADname",
            kind: "plugin",
            type: "ApiPlugin",
            source: "tafutaAPI"
        });

        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        await generateClientCommand.execute();
        expect((treeProvider.getSelectedPaths()).length).toEqual(1);
        expect(treeProvider.descriptionUrl).toBeTruthy();
        expect(vscodeWindowSpy).not.toHaveBeenCalled();
        expect(getlanguageInfoFn).toHaveBeenCalled();
        const stateInfo = await transformToGenerationConfig({
            name: "OverridingRepairsOADname",
            kind: "plugin",
            type: "ApiPlugin",
            source: "tafutaAPI"
        });
        expect(generateStepsFn).toHaveBeenCalledWith(stateInfo, undefined, {
            name: "OverridingRepairsOADname",
            kind: "plugin",
            type: "ApiPlugin",
            source: "tafutaAPI"
        });
        expect(showUpgradeWarningMessageStub).toHaveBeenCalled();
        expect(getExtensionSettingsStub).toHaveBeenCalledWith("kiota");
    });

    test('test ttk integration in function execute of GenerateClientCommand', async () => {
        const treeProvider = jest.createMockFromModule<treeModule.OpenApiTreeProvider>("../../../providers/openApiTreeProvider");
        Object.defineProperty(treeProvider, "descriptionUrl", {
            get: jest.fn().mockReturnValue("https://graph.microsoft.com/v1.0/$metadata")
        });
        treeProvider.getSelectedPaths = jest.fn().mockReturnValue(["repairs"]);
        treeProvider.apiTitle = "Repairs OAD";
        const sanitizedApiTitle = getSanitizedString(treeProvider.apiTitle);
        const viewProvider = jest.createMockFromModule<dependenciesModule.DependenciesViewProvider>("../../../providers/dependenciesViewProvider");
        const vscodeWindowSpy = jest.spyOn(vscode.window, "showErrorMessage").mockResolvedValue(undefined);
        const getlanguageInfoFn = jest.spyOn(languageInfoModule, "getLanguageInformation").mockResolvedValue(undefined);
        jest.spyOn(msgUtilitiesModule, "showUpgradeWarningMessage").mockResolvedValue(undefined);
        const clearDeepLinkParamSpy = jest.spyOn(deepLinkParamsHandler, "clearDeepLinkParams");
        jest.spyOn(settingsModule, "getExtensionSettings").mockReturnValue(extensionSettings);
        const generateStepsFn = jest.spyOn(generateStepsModule, "generateSteps").mockResolvedValue({
            generationType: "apimanifest",
            outputPath: "path/to/temp/folder",
            pluginName: sanitizedApiTitle
        });
        deepLinkParamsHandler.setDeepLinkParams({
            kind: "plugin",
            type: "apimanifest",
            source: "TTK",
            ttkContext: {
                lastCommand: 'createDeclarativeCopilotWithManifest'
            }
        });

        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        const outputPath = path.join("path", "to", "temp", "folder", "appPackage");
        const executeCommandStub = jest.spyOn(vscode.commands, "executeCommand").mockResolvedValue(undefined);
        await generateClientCommand.execute();

        expect(vscodeWindowSpy).not.toHaveBeenCalled();
        expect(getlanguageInfoFn).toHaveBeenCalled();
        const updatedDeepLinkParams: Partial<IntegrationParams> = {
            kind: "plugin",
            type: "apimanifest",
            source: "TTK",
            name: sanitizedApiTitle
        };
        expect(generateStepsFn).toHaveBeenCalledWith(expect.objectContaining({
            generationType: "apimanifest",
            outputPath: "path/to/temp/folder",
            pluginName: sanitizedApiTitle
        }), undefined, updatedDeepLinkParams);
        expect(executeCommandStub).toHaveBeenCalledWith(
            'fx-extension.createprojectfromkiota',
            [
                path.join(outputPath, `${sanitizedApiTitle?.toLowerCase()}-openapi.yml`),
                path.join(outputPath, `${sanitizedApiTitle?.toLowerCase()}-apiplugin.json`),
                { lastCommand: 'createDeclarativeCopilotWithManifest' }
            ]
        );
        expect(clearDeepLinkParamSpy).toHaveBeenCalled();

        // Test call to ttk createprojectfromkiota fails with undefined ttkContext Param
        deepLinkParamsHandler.setDeepLinkParams({
            kind: "plugin",
            type: "apimanifest",
            source: "TTK"
        });
        executeCommandStub.mockRejectedValue(new Error("ttk context not provided"));
        const telemetryStub = jest.spyOn(TelemetryReporter.prototype, "sendTelemetryEvent").mockImplementation(() => { });
        await generateClientCommand.execute();
        expect(telemetryStub).toHaveBeenCalledWith(
            "DeepLinked fx-extension.createprojectfromkiota",
            { "error": '{"name":"ttk context not provided"}' }
        );
    });
});