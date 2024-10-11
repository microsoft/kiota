// You can import and use all API from the 'vscode' module
// as well as import your extension to test it
// import { assert } from "chai";
import assert from "assert";
import * as sinon from "sinon";
import * as vscode from 'vscode';
import * as generateModule from "../../../commands/generate/generateClientCommand";
import * as dependenciesModule from "../../../dependenciesViewProvider";
import * as treeModule from "../../../openApiTreeProvider";


let context: vscode.ExtensionContext = {
    subscriptions: [],
    workspaceState: {} as vscode.Memento,
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
    extension: {} as vscode.Extension<any>
};

suite('GenerateClientCommand Test Suite', () => {
	void vscode.window.showInformationMessage('Start GenerateClientCommand tests.');
    const sanbox = sinon.createSandbox();

    teardown(async () => {
        sanbox.restore();
    });

    test('test function getName of GenerateClientCommand', () => {
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        var viewProvider = sinon.createStubInstance(dependenciesModule.DependenciesViewProvider);
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider);
        assert.strictEqual("kiota.openApiExplorer.generateClient", generateClientCommand.getName());
    });

    test('test function execute of GenerateClientCommand with 0 selected paths', async () => {
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        treeProvider.getSelectedPaths.returns([]);
        var viewProvider = sinon.createStubInstance(dependenciesModule.DependenciesViewProvider);
        const vscodeWindowSpy = sinon.stub(vscode.window, "showErrorMessage");
        const generateClientCommand = new generateModule.GenerateClientCommand(treeProvider, context, viewProvider);
        await generateClientCommand.execute();
        assert.strictEqual((treeProvider.getSelectedPaths()).length, 0);
        sinon.assert.calledOnceWithMatch(vscodeWindowSpy, vscode.l10n.t("No endpoints selected, select endpoints first"));
        // sinon.assert.calledWith(filterStepsStub, treeProvider.filter, sinon.match.func);
    });
});
