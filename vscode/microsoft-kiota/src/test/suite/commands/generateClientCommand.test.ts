// You can import and use all API from the 'vscode' module
// as well as import your extension to test it
import { KiotaGenerationLanguage, KiotaLogEntry } from "@microsoft/kiota";
import TelemetryReporter from "@vscode/extension-telemetry";
import * as path from "path";
import * as vscode from 'vscode';

import * as generateModule from "../../../commands/generate/generateClientCommand";
import * as deepLinkParamsHandler from "../../../handlers/deepLinkParamsHandler";
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
    languageModelAccessInformation: {} as any,
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
    includeAdditionalData: false,
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

let result: KiotaLogEntry[] = [{ level: 1, message: "Parsing OpenAPI file" }, { level: 2, message: "Generation completed successfully" }];

const setWorkspaceGenerationContext = (params: Partial<WorkspaceGenerationContext>): void => { };

suite('GenerateClientCommand Test Suite', () => {
    const sanbox = sinon.createSandbox();
    let myOutputChannel = vscode.window.createOutputChannel("Kiota", {
        log: true,
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

    test.skip('test function execute of GenerateClientCommand with descriptionUrl unset', async () => {
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        treeProvider.getSelectedPaths.returns(["repairs"]);
        treeProvider.apiTitle = "Repairs OAD";
        var viewProvider = sinon.createStubInstance(dependenciesModule.DependenciesViewProvider);
        const vscodeWindowSpy = sinon.mock(vscode.window).expects(
            "showErrorMessage").once().withArgs(
                vscode.l10n.t("No description found, select a description first")
            );
        let config: Partial<generateStepsModule.GenerateState> = { generationType: "client" };
        const generateStepsFn = sinon.stub(generateStepsModule, "generateSteps");
        generateStepsFn.resolves(config);
        const showUpgradeWarningMessageStub = sinon.stub(msgUtilitiesModule, "showUpgradeWarningMessage");
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        await generateClientCommand.execute();
        assert.strictEqual((treeProvider.getSelectedPaths()).length, 1);
        vscodeWindowSpy.verify();
        let stateInfo: Partial<generateStepsModule.GenerateState> = {
            clientClassName: treeProvider.clientClassName,
            clientNamespaceName: treeProvider.clientNamespaceName,
            language: treeProvider.language,
            outputPath: treeProvider.outputPath,
            pluginName: "RepairsOAD"
        };
        assert.strictEqual(!treeProvider.descriptionUrl, true);
        sinon.assert.calledOnceWithMatch(generateStepsFn, stateInfo, undefined, {});
        sinon.assert.calledOnce(showUpgradeWarningMessageStub);
        sinon.restore();
    });

    test.skip('test successful completion of function execute of GenerateClientCommand', async () => {
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        sinon.stub(
            treeProvider, "descriptionUrl"
        ).get(
            function getterFn() {
                return "https://graph.microsoft.com/v1.0/$metadata";
            }
        );
        treeProvider.getSelectedPaths.returns(["repairs"]);
        treeProvider.apiTitle = "Repairs OAD";
        var viewProvider = sinon.createStubInstance(dependenciesModule.DependenciesViewProvider);
        const showUpgradeWarningMessageStub = sinon.stub(msgUtilitiesModule, "showUpgradeWarningMessage");
        const getExtensionSettingsStub = sinon.stub(settingsModule, "getExtensionSettings").onFirstCall().returns(extensionSettings);
        //set deeplinkparams with name provided.
        var pluginParams: any = {
            name: "OverridingRepairsOADname", //sanitized before setDeepLinkParams is called
            kind: "plugin",
            type: "ApiPlugin",
            source: "tafutaAPI"
        };
        let config: Partial<generateStepsModule.GenerateState> = { generationType: "plugin", outputPath: "path/to/temp/folder", pluginName: pluginParams.name };
        const generateStepsFn = sinon.stub(generateStepsModule, "generateSteps");
        generateStepsFn.resolves(config);
        deepLinkParamsHandler.setDeepLinkParams(pluginParams);

        //stub and call generateCommand
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        await generateClientCommand.execute();
        assert.strictEqual((treeProvider.getSelectedPaths()).length, 1);
        assert.strictEqual(!treeProvider.descriptionUrl, false);
        let stateInfo = await transformToGenerationConfig(pluginParams);
        sinon.assert.calledOnceWithMatch(generateStepsFn, stateInfo, undefined, pluginParams);
        sinon.assert.calledOnce(showUpgradeWarningMessageStub);
        sinon.assert.calledOnceWithMatch(getExtensionSettingsStub, "kiota");

        // assert successful call to method generatePluginAndRefreshUI
        generatePluginAndRefreshUIExpectation.verify();
        sinon.restore();
    });

    test.skip('test ttk integration in function execute of GenerateClientCommand', async () => {
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        sinon.stub(
            treeProvider, "descriptionUrl"
        ).get(
            function getterFn() {
                return "https://graph.microsoft.com/v1.0/$metadata";
            }
        );
        treeProvider.getSelectedPaths.returns(["repairs"]);
        treeProvider.apiTitle = "Repairs OAD";
        let sanitizedApiTitle = getSanitizedString(treeProvider.apiTitle);
        var viewProvider = sinon.createStubInstance(dependenciesModule.DependenciesViewProvider);
        const vscodeWindowSpy = sinon.mock(vscode.window).expects("showErrorMessage").never();
        sinon.stub(msgUtilitiesModule, "showUpgradeWarningMessage");
        const clearDeepLinkParamSpy = sinon.spy(deepLinkParamsHandler, "clearDeepLinkParams");
        sinon.stub(settingsModule, "getExtensionSettings").returns(extensionSettings);
        var pluginParams: any = {
            kind: "plugin",
            type: "apimanifest",
            source: "TTK",
            ttkContext: {
                lastCommand: 'createDeclarativeCopilotWithManifest'
            }
        };
        let config: Partial<generateStepsModule.GenerateState> = { generationType: "apimanifest", outputPath: "path/to/temp/folder", pluginName: sanitizedApiTitle };
        const generateStepsFn = sinon.stub(generateStepsModule, "generateSteps");
        generateStepsFn.resolves(config);
        deepLinkParamsHandler.setDeepLinkParams(pluginParams);

        //stub and call generateCommand
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider, setWorkspaceGenerationContext, myOutputChannel);
        const outputPath = path.join("path", "to", "temp", "folder", "appPackage");
        const executeCommandStub = jest.spyOn(vscode.commands, "executeCommand").mockResolvedValue(undefined);
        await generateClientCommand.execute();

        // assertions
        vscodeWindowSpy.verify();
        var updatedDeepLinkParams: Partial<IntegrationParams> = JSON.parse(JSON.stringify(pluginParams));
        updatedDeepLinkParams["name"] = sanitizedApiTitle;
        sinon.assert.calledOnce(generateStepsFn);
        sinon.assert.calledWithMatch(
            executeCommandStub,
            'fx-extension.createprojectfromkiota',
            [
                path.join(outputPath, `${sanitizedApiTitle?.toLowerCase()}-openapi.yml`),
                path.join(outputPath, `${sanitizedApiTitle?.toLowerCase()}-apiplugin.json`),
                { lastCommand: 'createDeclarativeCopilotWithManifest' }
                { lastCommand: 'createDeclarativeCopilotWithManifest' }
            ]
        );
        expect(clearDeepLinkParamSpy).toHaveBeenCalled();

        // Test call to ttk createprojectfromkiota fails with undefined ttkContext Param
        deepLinkParamsHandler.setDeepLinkParams({
            kind: "plugin",
            type: "apimanifest",
            source: "TTK",
        };
        deepLinkParamsHandler.setDeepLinkParams(pluginParams);
        executeCommandStub.throws("ttk context not provided");
        const telemetryStub = sinon.stub(TelemetryReporter.prototype, "sendTelemetryEvent").resolves();
        //call execute command again but this time expect call to fail
        await generateClientCommand.execute();
        expect(telemetryStub).toHaveBeenCalledWith(
            "DeepLinked fx-extension.createprojectfromkiota",
            { "error": '{"name":"ttk context not provided"}' }
            { "error": '{"name":"ttk context not provided"}' }
        );
    });
});