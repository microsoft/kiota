import * as vscode from 'vscode';
import { l10n } from 'vscode';

export class CodeLensProvider implements vscode.CodeLensProvider {
    public provideCodeLenses(document: vscode.TextDocument, token: vscode.CancellationToken): vscode.ProviderResult<vscode.CodeLens[]> {
        const codeLenses: vscode.CodeLens[] = [];
        const text = document.getText();
        const jsonObject = JSON.parse(text);

        if (document.fileName.endsWith('workspace.json')) {
            ['clients', 'plugins'].forEach(objectKey => {
                const object = jsonObject[objectKey];
                if (object) {
                    Object.keys(object).forEach(key => {
                        const obj = object[key];
                        const startLine = this.findPropertyLine(text, key);
                        if (startLine !== -1) {
                            const positionBeforeObj = new vscode.Position(startLine, 0);
                            const rangeBeforeObj = new vscode.Range(positionBeforeObj, positionBeforeObj);
    
                            const editPathsCommand = {
                                title: l10n.t("Edit Paths"),
                                command: "kiota.editPaths",
                                arguments: [key, obj, objectKey]
                            };
                            codeLenses.push(new vscode.CodeLens(rangeBeforeObj, editPathsCommand));
    
                            const regenerateCommand = {
                                title: l10n.t("Re-generate"),
                                command: "kiota.regenerate",
                                arguments: [key, obj, objectKey]
                            };
                            codeLenses.push(new vscode.CodeLens(rangeBeforeObj, regenerateCommand));
                        }
                    });
                }
            });
        }
        return codeLenses;
    }

    private findPropertyLine(text: string, property: string): number {
        const propertyRegex = new RegExp(`"${property}"\\s*:\\s*{`);
        const match = text.match(propertyRegex);
        return match ? text.substr(0, match.index).split('\n').length - 1 : -1;
    }
}