// You can import and use all API from the 'vscode' module
// as well as import your extension to test it
import assert from "assert";
import * as sinon from "sinon";
import * as vscode from 'vscode';
import * as filterModule from "../../../commands/openApiTreeView/filterDescriptionCommand";
import * as filterStepsModule from "../../../modules/steps/filterSteps";
import * as treeModule from "../../../providers/openApiTreeProvider";

suite('FilterDescriptionCommand Test Suite', () => {
    void vscode.window.showInformationMessage('Start FilterDescriptionCommand tests.');
    const sanbox = sinon.createSandbox();

    teardown(async () => {
        sanbox.restore();
    });

    test('test function getName of filterDescriptionCommand', () => {
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const filterDescriptionCommand = new filterModule.FilterDescriptionCommand(treeProvider);
        assert.strictEqual("kiota.openApiExplorer.filterDescription", filterDescriptionCommand.getName());
    });

    test('test function execute of filterDescriptionCommand', async () => {
        const filterStepsStub = sanbox.stub(filterStepsModule, 'filterSteps');
        var treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const filterDescriptionCommand = new filterModule.FilterDescriptionCommand(treeProvider);
        await filterDescriptionCommand.execute();
        sinon.assert.calledOnce(filterStepsStub);
        sinon.assert.calledWith(filterStepsStub, treeProvider.filter, sinon.match.func);
    });
});