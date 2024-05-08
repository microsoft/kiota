import * as vscode from 'vscode';

export class CodeLensProvider implements vscode.CodeLensProvider {
    public provideCodeLenses(document: vscode.TextDocument, token: vscode.CancellationToken): vscode.ProviderResult<vscode.CodeLens[]> {
        const codeLenses: vscode.CodeLens[] = [];
        const text = document.getText();
        const jsonObject = JSON.parse(text);

        if (document.fileName.endsWith('workspace.json')) {
            const clientsObject = jsonObject['clients'];
            if (clientsObject) {
                const clientsStartLine = this.findPropertyLine(text, "clients");
                if (clientsStartLine !== -1) {
                    const clientKeys = Object.keys(clientsObject);
                    clientKeys.forEach(clientKey => {
                        const clientObject = clientsObject[clientKey];
                        const clientStartLine = this.findPropertyLine(text, clientKey);
                        if (clientStartLine !== -1) {
                            const positionBeforeClient = new vscode.Position(clientStartLine, 0);
                            const rangeBeforeClient = new vscode.Range(positionBeforeClient, positionBeforeClient);
                            const editPathsCommand = {
                                title: "Edit Paths",
                                command: "kiota.editPaths",
                                arguments: [document.fileName, clientObject] 
                            };
                            codeLenses.push(new vscode.CodeLens(rangeBeforeClient, editPathsCommand));
                            const regenerateCommand = {
                                title: "Re-generate",
                                command: "kiota.regenerate",
                                arguments: [document.fileName, clientKey]
                            };
                            codeLenses.push(new vscode.CodeLens(rangeBeforeClient, regenerateCommand));
                        }
                    });
                }
            }
        }
        return codeLenses;
    }

    private findPropertyLine(text: string, property: string): number {
        const propertyRegex = new RegExp(`"${property}"\\s*:\\s*{`);
        const match = text.match(propertyRegex);
        return match ? text.substr(0, match.index).split('\n').length - 1 : -1;
    }
}