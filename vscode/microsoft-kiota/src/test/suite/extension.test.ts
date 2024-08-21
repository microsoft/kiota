import * as assert from 'assert';

import * as vscode from 'vscode';

suite('Extension Test Suite', () => {
	void vscode.window.showInformationMessage('Start all tests.');

	test('Sample test', () => {
		assert.strictEqual(-1, [1, 2, 3].indexOf(5));
		assert.strictEqual(-1, [1, 2, 3].indexOf(0));
	});

    test('test function getQueryParameters', async () => {
        var sampleUri = "vscode://ms-graph.kiota/OpenDescription?descriptionUrl%3Dhttps%3A%2F%2Fraw.githubusercontent.com%2Fgithub%2Frest-api-description%2Fmain%2Fdescriptions%2Fghes-3.0%2Fghes-3.0.json&kind=Plugin&type=ApiPlugin&name=GitHubPlugin&source=ttk";
        const vscodeUri = vscode.Uri.parse(sampleUri);
        assert.strictEqual(vscodeUri.path, "/OpenDescription");
        // await vscode.commands.executeCommand("editor.action.openLink", sampleUri);
        await vscode.commands.executeCommand("extension.js-debug.debugLink", sampleUri);

    });

    test('test function getQueryParameters with APIManifest type', async () => {
        var sampleUri = "vscode://ms-graph.kiota/OpenDescription?descriptionUrl%3Dhttps%3A%2F%2Fraw.githubusercontent.com%2Fgithub%2Frest-api-description%2Fmain%2Fdescriptions%2Fghes-3.0%2Fghes-3.0.json&kind=plugin&type=ApiManifest&name=GitHubManifest&source=ttk";
        const vscodeUri = vscode.Uri.parse(sampleUri);
        assert.strictEqual(vscodeUri.path, "/OpenDescription");
        await vscode.commands.executeCommand("extension.js-debug.debugLink", sampleUri);

    });

    test('test function getQueryParameters with client kind', async () => {
        var sampleUri = "vscode://ms-graph.kiota/OpenDescription?descriptionUrl%3Dhttps%3A%2F%2Fraw.githubusercontent.com%2Fgithub%2Frest-api-description%2Fmain%2Fdescriptions%2Fghes-3.0%2Fghes-3.0.json&kind=client&language=Python&name=GitHubClient&source=ttk";
        const vscodeUri = vscode.Uri.parse(sampleUri);
        assert.strictEqual(vscodeUri.path, "/OpenDescription");
        await vscode.commands.executeCommand("extension.js-debug.debugLink", sampleUri);

    });
});
