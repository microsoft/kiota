import { DependencyType, KiotaGenerationLanguage, LanguageInformation, MaturityLevel } from "@microsoft/kiota";
import assert from "assert";
import * as vscode from 'vscode';

import { DependenciesViewProvider } from "../../../providers/dependenciesViewProvider";

function createFakeWebview(): vscode.Webview {
    return {
        cspSource: "vscode-webview://fake-csp-source",
        asWebviewUri: (uri: vscode.Uri) => uri,
        html: "",
        options: {},
        onDidReceiveMessage: (() => ({ dispose: () => { } })) as any,
        postMessage: (async () => true) as any,
    } as unknown as vscode.Webview;
}

suite('DependenciesViewProvider Test Suite', () => {
    const extensionUri = vscode.Uri.parse('file:///fake-extension');

    test('escapes malicious dependency name/version to prevent XSS', () => {
        const maliciousLanguageInformation: LanguageInformation = {
            // eslint-disable-next-line @typescript-eslint/naming-convention
            MaturityLevel: MaturityLevel.stable,
            // eslint-disable-next-line @typescript-eslint/naming-convention
            ClientNamespaceName: 'ns',
            // eslint-disable-next-line @typescript-eslint/naming-convention
            ClientClassName: 'cls',
            // eslint-disable-next-line @typescript-eslint/naming-convention
            StructuredMimeTypes: [],
            // eslint-disable-next-line @typescript-eslint/naming-convention
            DependencyInstallCommand: 'install {0}@{1}',
            // eslint-disable-next-line @typescript-eslint/naming-convention
            Dependencies: [
                {
                    // eslint-disable-next-line @typescript-eslint/naming-convention
                    Name: '<script>alert(1)</script>',
                    // eslint-disable-next-line @typescript-eslint/naming-convention
                    Version: '"><img src=x onerror=alert(2)>',
                    // eslint-disable-next-line @typescript-eslint/naming-convention
                    DependencyType: DependencyType.bundle
                }
            ]
        };

        const provider = new DependenciesViewProvider(extensionUri, maliciousLanguageInformation, KiotaGenerationLanguage.CSharp);
        const webview = createFakeWebview();
        // resolveWebviewView sets webview.html via the private _getHtmlForWebview method
        (provider as any).resolveWebviewView({ webview, show: () => { } } as unknown as vscode.WebviewView, {} as vscode.WebviewViewResolveContext<unknown>, {} as vscode.CancellationToken);

        const html = webview.html;

        assert.ok(!html.includes('<script>alert(1)</script>'), 'raw script tag must not appear unescaped');
        assert.ok(!html.includes('<img src=x onerror=alert(2)>'), 'raw injected img tag must not appear unescaped');
        assert.ok(html.includes('&lt;script&gt;'), 'dependency name should be HTML-escaped');
        assert.ok(html.includes('&quot;&gt;&lt;img'), 'dependency version should be HTML-escaped');
    });

    test('includes a Content-Security-Policy meta tag', () => {
        const languageInformation: LanguageInformation = {
            // eslint-disable-next-line @typescript-eslint/naming-convention
            MaturityLevel: MaturityLevel.stable,
            // eslint-disable-next-line @typescript-eslint/naming-convention
            ClientNamespaceName: 'ns',
            // eslint-disable-next-line @typescript-eslint/naming-convention
            ClientClassName: 'cls',
            // eslint-disable-next-line @typescript-eslint/naming-convention
            StructuredMimeTypes: [],
            // eslint-disable-next-line @typescript-eslint/naming-convention
            DependencyInstallCommand: 'install {0}@{1}',
            // eslint-disable-next-line @typescript-eslint/naming-convention
            Dependencies: [
                // eslint-disable-next-line @typescript-eslint/naming-convention
                { Name: 'SomeDep', Version: '1.0.0', DependencyType: DependencyType.bundle }
            ]
        };

        const provider = new DependenciesViewProvider(extensionUri, languageInformation, KiotaGenerationLanguage.CSharp);
        const webview = createFakeWebview();
        (provider as any).resolveWebviewView({ webview, show: () => { } } as unknown as vscode.WebviewView, {} as vscode.WebviewViewResolveContext<unknown>, {} as vscode.CancellationToken);

        assert.ok(webview.html.includes('Content-Security-Policy'), 'expected a CSP meta tag in the webview HTML');
        assert.ok(webview.html.includes("script-src 'nonce-"), 'expected script-src to be restricted to a nonce');
    });
});
