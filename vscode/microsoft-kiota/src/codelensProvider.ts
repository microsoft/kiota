import * as vscode from 'vscode';

export class CodeLensProvider implements vscode.CodeLensProvider {
    public provideCodeLenses(document: vscode.TextDocument, token: vscode.CancellationToken): vscode.ProviderResult<vscode.CodeLens[]> {
        console.log("provideCodeLenses called");
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
                        const clientStartLine = this.findPropertyLine(text, clientKey);
                        if (clientStartLine !== -1) {
                            const positionBeforeClient = new vscode.Position(clientStartLine, 0);
                            const rangeBeforeClient = new vscode.Range(positionBeforeClient, positionBeforeClient);
                            const commandBeforeClient = {
                                title: "Edit Paths",
                                command: "kiota.editPaths",
                                arguments: [document.fileName, clientKey] 
                            };
                            codeLenses.push(new vscode.CodeLens(rangeBeforeClient, commandBeforeClient));
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