// You can import and use all API from the 'vscode' module
// as well as import your extension to test it
// import { assert } from "chai";
import assert from "assert";
import * as sinon from "sinon";
import * as vscode from 'vscode';
import * as filterModule from "../../commands/open-api-tree-view/filterDescriptionCommand";
import * as treeModule from "../../openApiTreeProvider";
import * as stepsModule from "../../steps";


suite('Extension Test Suite', () => {
	void vscode.window.showInformationMessage('Start all tests.');
    const sanbox = sinon.createSandbox();

    teardown(async () => {
        sanbox.restore();
    });

    test('Sample test', () => {
        assert.strictEqual(-1, [1, 2, 3].indexOf(5));
        assert.strictEqual(-1, [1, 2, 3].indexOf(0));
    });

    test('test function getName of filterDescriptionCommand', () => {
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const filterDescriptionCommand = new filterModule.FilterDescriptionCommand(treeProvider);
        assert.strictEqual("kiota.openApiExplorer.filterDescription", filterDescriptionCommand.getName());
    });

    test('test function execute of filterDescriptionCommand', async () => {
        const filterStepsStub = sanbox.stub(stepsModule, 'filterSteps');
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const filterDescriptionCommand = new filterModule.FilterDescriptionCommand(treeProvider);
        await filterDescriptionCommand.execute();
        sinon.assert.calledOnce(filterStepsStub);
        sinon.assert.calledWith(filterStepsStub, treeProvider.filter, sinon.match.func);
    });
});
