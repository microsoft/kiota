// You can import and use all API from the 'vscode' module
// as well as import your extension to test it
import assert from "assert";
import * as sinon from "sinon";
import * as vscode from 'vscode';
import * as fs from "fs";

import * as loadModule from "../../../commands/openApidescription/loadOpenApiDescriptionFromFileCommand";
import * as treeModule from "../../../providers/openApiTreeProvider";
import * as kiotaModule from "@microsoft/kiota";

suite('LoadOpenApiDescriptionFromFileCommand Test Suite', () => {
    void vscode.window.showInformationMessage('Start LoadOpenApiDescriptionFromFileCommand tests.');
    const sandbox = sinon.createSandbox();

    teardown(async () => {
        sandbox.restore();
    });

    test('test function getName of loadOpenApiDescriptionFromFileCommand', () => {
        const treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const mockContext = {} as vscode.ExtensionContext;
        const loadCommand = new loadModule.LoadOpenApiDescriptionFromFileCommand(treeProvider, mockContext);
        assert.strictEqual("kiota.openApiExplorer.loadOpenApiDescriptionFromFile", loadCommand.getName());
    });

    test('test isOpenApiContent detects OpenAPI files correctly', () => {
        const treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const mockContext = {} as vscode.ExtensionContext;
        const loadCommand = new loadModule.LoadOpenApiDescriptionFromFileCommand(treeProvider, mockContext);
        
        // Access the private method for testing
        const command = loadCommand as any;
        
        // Test positive cases
        assert.strictEqual(true, command.isOpenApiContent('openapi: 3.0.1'));
        assert.strictEqual(true, command.isOpenApiContent('swagger: "2.0"'));
        assert.strictEqual(true, command.isOpenApiContent('{\n  "openapi": "3.0.1"\n}'));
        assert.strictEqual(true, command.isOpenApiContent('{\n  "swagger": "2.0"\n}'));
        
        // Test negative cases
        assert.strictEqual(false, command.isOpenApiContent('version: 1.0\nname: test'));
        assert.strictEqual(false, command.isOpenApiContent('apiVersion: v1\nkind: Deployment'));
    });

    test('test execute with non-existing file shows error', async () => {
        const treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const mockContext = {
            extension: { packageJSON: { telemetryInstrumentationKey: 'test-key' } }
        } as any;
        
        // Mock fs.existsSync to return false
        const existsSyncStub = sandbox.stub(fs, 'existsSync').returns(false);
        const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
        
        const loadCommand = new loadModule.LoadOpenApiDescriptionFromFileCommand(treeProvider, mockContext);
        const mockUri = { fsPath: '/non/existing/file.yaml' } as vscode.Uri;
        
        await loadCommand.execute(mockUri);
        
        sinon.assert.calledOnce(existsSyncStub);
        sinon.assert.calledOnce(showErrorMessageStub);
        sinon.assert.calledWith(showErrorMessageStub, sinon.match(/File not found/));
    });

    test('test execute with non-YAML file shows error', async () => {
        const treeProvider = sinon.createStubInstance(treeModule.OpenApiTreeProvider);
        const mockContext = {
            extension: { packageJSON: { telemetryInstrumentationKey: 'test-key' } }
        } as any;
        
        // Mock fs.existsSync to return true
        const existsSyncStub = sandbox.stub(fs, 'existsSync').returns(true);
        const showErrorMessageStub = sandbox.stub(vscode.window, 'showErrorMessage');
        
        const loadCommand = new loadModule.LoadOpenApiDescriptionFromFileCommand(treeProvider, mockContext);
        const mockUri = { fsPath: '/some/file.json' } as vscode.Uri;
        
        await loadCommand.execute(mockUri);
        
        sinon.assert.calledOnce(existsSyncStub);
        sinon.assert.calledOnce(showErrorMessageStub);
        sinon.assert.calledWith(showErrorMessageStub, sinon.match(/must be a YAML file/));
    });
});