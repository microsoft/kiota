import * as vscode from 'vscode';
import { generationLanguageToString, KiotaGenerationLanguage, LanguageInformation, LanguagesInformation } from './kiotaInterop';

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
        const languageInformation = languagesInformation[generationLanguageToString(language)];
        this._languageInformation = languageInformation;
        this._language = language;
        if(this._view) {
            this._view.show?.(true);
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
        const installationBlock = this._languageInformation?.DependencyInstallCommand ? `<h2>${installationCommands}</h2>
            <pre>${this._languageInformation.Dependencies.map(dep => this._languageInformation!.DependencyInstallCommand.replace(/\{0\}/g, dep.Name).replace(/\{1\}/g, dep.Version)).join('\n')}</pre>`
        : '';

		return `<!DOCTYPE html>
			<html lang="en">
			<head>
				<meta charset="UTF-8">
				<meta name="viewport" content="width=device-width, initial-scale=1.0">
				<link href="${styleResetUri}" rel="stylesheet">
				<link href="${styleVSCodeUri}" rel="stylesheet">
				<title>${title}</title>
			</head>
			<body>
                <h1>${this._language !== undefined ? generationLanguageToString(this._language) : noLanguageSelected}</h1>
                <h2>${dependencies}</h2>
				<ul>
                    ${this._languageInformation ? this._languageInformation?.Dependencies.map(dep => `<li>${dep.Name} (${dep.Version})</li>`).join('') : ''}
				</ul>
                ${installationBlock}
			</body>
			</html>`;
	}

}