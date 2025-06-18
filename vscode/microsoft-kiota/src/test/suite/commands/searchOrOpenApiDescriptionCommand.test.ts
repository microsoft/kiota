import { TelemetryReporter } from "@vscode/extension-telemetry";
import assert from "assert";
import * as sinon from "sinon";
import * as vscode from 'vscode';

import { SearchOrOpenApiDescriptionCommand } from "../../../commands/openApidescription/searchOrOpenApiDescriptionCommand";
import * as deepLinkParamsHandler from "../../../handlers/deepLinkParamsHandler";
import * as searchStepsModule from "../../../modules/steps/searchSteps";
import * as openApiTreeProviderModule from "../../../providers/openApiTreeProvider";
import { IntegrationParams } from "../../../utilities/deep-linking";
import * as progressModule from "../../../utilities/progress";

let context: vscode.ExtensionContext = {
    subscriptions: [],
    workspaceState: {
        update: sinon.stub().resolves(),
        keys: sinon.stub().returns([]),
        get: sinon.stub().returns(undefined)
    } as vscode.Memento,
    globalState: {
        update: sinon.stub().resolves(),
        keys: sinon.stub().returns([]),
        get: sinon.stub().returns(false),
        setKeysForSync: sinon.stub()
    } as vscode.Memento & { setKeysForSync(keys: readonly string[]): void; },
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

describe('SearchOrOpenApiDescriptionCommand', () => {
    let openApiTreeProvider: openApiTreeProviderModule.OpenApiTreeProvider;
    let command: SearchOrOpenApiDescriptionCommand;
    let searchStepsStub: sinon.SinonStub;
    let openProgressStub: sinon.SinonStub;
    let setDescriptionUrlStub: sinon.SinonStub;
    let hasChangesStub: sinon.SinonStub;

    beforeEach(() => {
        // Create mock OpenApiTreeProvider
        openApiTreeProvider = {} as openApiTreeProviderModule.OpenApiTreeProvider;
        hasChangesStub = sinon.stub().returns(false);
        setDescriptionUrlStub = sinon.stub().resolves();
        openApiTreeProvider.hasChanges = hasChangesStub;
        openApiTreeProvider.setDescriptionUrl = setDescriptionUrlStub;

        command = new SearchOrOpenApiDescriptionCommand(openApiTreeProvider, context);

        // Mock the searchSteps function
        searchStepsStub = sinon.stub(searchStepsModule, 'searchSteps').resolves({
            descriptionPath: undefined
        });

        // Mock openTreeViewWithProgress
        openProgressStub = sinon.stub(progressModule, 'openTreeViewWithProgress').callsFake(async (callback: () => Promise<unknown>) => {
            await callback();
        });

        // Clean up deep link params
        deepLinkParamsHandler.clearDeepLinkParams();
    });

    afterEach(() => {
        sinon.restore();
    });

    it('should call searchSteps when no descriptionurl is provided', async () => {
        const searchParams: Partial<IntegrationParams> = {
            name: 'TestClient',
            kind: 'client'
        };

        await command.execute(searchParams);

        assert(searchStepsStub.calledOnce, 'searchSteps should be called when no descriptionurl is provided');
    });

    it('should skip searchSteps when descriptionurl is provided', async () => {
        const searchParams: Partial<IntegrationParams> = {
            descriptionurl: 'https://example.com/openapi.json',
            name: 'TestClient',
            kind: 'client'
        };

        await command.execute(searchParams);

        assert(searchStepsStub.notCalled, 'searchSteps should NOT be called when descriptionurl is provided');
        assert(setDescriptionUrlStub.calledOnceWith('https://example.com/openapi.json'), 'setDescriptionUrl should be called with the provided URL');
    });

    it('should handle local file paths in descriptionurl', async () => {
        const searchParams: Partial<IntegrationParams> = {
            descriptionurl: '/path/to/local/openapi.yaml',
            name: 'TestClient',
            kind: 'client'
        };

        await command.execute(searchParams);

        assert(searchStepsStub.notCalled, 'searchSteps should NOT be called when descriptionurl with local path is provided');
        assert(setDescriptionUrlStub.calledOnceWith('/path/to/local/openapi.yaml'), 'setDescriptionUrl should be called with the provided local path');
    });

    it('should call searchSteps when descriptionurl is empty', async () => {
        const searchParams: Partial<IntegrationParams> = {
            descriptionurl: '',
            name: 'TestClient',
            kind: 'client'
        };

        await command.execute(searchParams);

        assert(searchStepsStub.calledOnce, 'searchSteps should be called when descriptionurl is empty');
    });
});