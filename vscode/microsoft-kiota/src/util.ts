import * as vscode from 'vscode';

export async function updateTreeViewIcons(treeViewId: string, showIcons: boolean, showRegenerateIcon?: boolean) {
    await vscode.commands.executeCommand('setContext', `${treeViewId}.showIcons`, showIcons);
    if (showRegenerateIcon !== undefined) {
        await vscode.commands.executeCommand('setContext', `${treeViewId}.showRegenerateIcon`, showRegenerateIcon);
    }
}