import {
    DependencyType, dependencyTypeToString, generationLanguageToString, KiotaGenerationLanguage,
    LanguageInformation, LanguagesInformation
} from '@microsoft/kiota';
import * as vscode from 'vscode';

import { escapeHtml, getNonce } from '../utilities/html';

export class DependenciesViewProvider implements vscode.WebviewViewProvider {
    private _view?: vscode.WebviewView;
    public constructor(
        private readonly _extensionUri: vscode.Uri,
        private _languageInformation?: LanguageInformation,
        private _language?: KiotaGenerationLanguage
    ) { }
    public resolveWebviewView(webviewView: vscode.WebviewView, context: vscode.WebviewViewResolveContext<unknown>, token: vscode.CancellationToken): void | Thenable<void> {
        this._view = webviewView;
        webviewView.webview.options = {
            // Allow scripts in the webview
            enableScripts: true,

            localResourceRoots: [
                this._extensionUri
            ]
        };
        webviewView.webview.html = this._getHtmlForWebview(webviewView.webview);
    }
    public update(languagesInformation: LanguagesInformation, language: KiotaGenerationLanguage) {
        this._languageInformation = languagesInformation[generationLanguageToString(language)];
        this._language = language;
        if (this._view) {
            this._view.show(true);
            this._view.webview.html = this._getHtmlForWebview(this._view.webview);
        }
    }

    private _getHtmlForWebview(webview: vscode.Webview) {
        // Do the same for the stylesheet.
        const styleResetUri = webview.asWebviewUri(vscode.Uri.joinPath(this._extensionUri, 'media', 'views', 'deps', 'reset.css'));
        const styleVSCodeUri = webview.asWebviewUri(vscode.Uri.joinPath(this._extensionUri, 'media', 'views', 'deps', 'vscode.css'));

        const installationCommands = vscode.l10n.t('Installation commands');
        const noLanguageSelected = vscode.l10n.t('No language selected, select a language first');
        const title = vscode.l10n.t('Kiota Dependencies Information');
        const dependencies = vscode.l10n.t('Dependencies');
        const name = vscode.l10n.t('Name');
        const version = vscode.l10n.t('Version');
        const type = vscode.l10n.t('Type');
        let dependenciesList = this._languageInformation ?
            this._languageInformation.Dependencies :
            [];
        if (dependenciesList.filter(dep => dep.DependencyType === DependencyType.bundle).length > 0) {
            dependenciesList = dependenciesList.filter(dep => dep.DependencyType === DependencyType.bundle || dep.DependencyType === DependencyType.additional || dep.DependencyType === DependencyType.authentication);
        }
        const nonce = getNonce();
        const installationBlock = this._languageInformation?.DependencyInstallCommand ? `<h2>${escapeHtml(installationCommands)}</h2>
            <pre>${dependenciesList.map(dep => escapeHtml(this._languageInformation!.DependencyInstallCommand.replace(/\{0\}/g, dep.Name).replace(/\{1\}/g, dep.Version))).join('\n')}</pre>`
            : '';

        return `<!DOCTYPE html>
			<html lang="en">
			<head>
				<meta charset="UTF-8">
				<meta http-equiv="Content-Security-Policy" content="default-src 'none'; style-src ${webview.cspSource}; img-src ${webview.cspSource}; script-src 'nonce-${nonce}';">
				<meta name="viewport" content="width=device-width, initial-scale=1.0">
				<link href="${styleResetUri}" rel="stylesheet">
				<link href="${styleVSCodeUri}" rel="stylesheet">
				<title>${escapeHtml(title)}</title>
			</head>
			<body>
                <h1>${this._language !== undefined ? escapeHtml(generationLanguageToString(this._language)) : escapeHtml(noLanguageSelected)}</h1>
                <h2>${escapeHtml(dependencies)}</h2>
                <table>
                    <tr><th>${escapeHtml(name)}</th><th>${escapeHtml(version)}</th><th>${escapeHtml(type)}</th></tr>
                    ${dependenciesList.map(dep => `<tr><td>${escapeHtml(dep.Name)}</td><td>${escapeHtml(dep.Version)}</td><td>${escapeHtml(dependencyTypeToString(dep.DependencyType))}</td></tr>`).join('')}
                </table>
                ${installationBlock}
			</body>
			</html>`;
    }

}
