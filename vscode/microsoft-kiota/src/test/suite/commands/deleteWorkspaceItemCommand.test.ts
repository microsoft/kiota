import assert from "assert";
import * as sinon from "sinon";
import * as vscode from 'vscode';

import { DeleteWorkspaceItemCommand } from '../../../commands/deleteWorkspaceItem/deleteWorkspaceItemCommand';
import { WorkspaceTreeItem } from '../../../providers/workspaceTreeProvider';

suite('DeleteWorkspaceItemCommand Tests', () => {
  let context: vscode.ExtensionContext;
  let outputChannel: vscode.LogOutputChannel;
  let command: DeleteWorkspaceItemCommand;
  let workspaceTreeItem: WorkspaceTreeItem;

  setup(() => {
    context = { extension: { packageJSON: { telemetryInstrumentationKey: 'test-key' } } } as any;
    outputChannel = { appendLine: sinon.stub() } as any;
    command = new DeleteWorkspaceItemCommand(context, outputChannel);
    workspaceTreeItem = { label: 'test-item', category: 'plugin' } as any;
  });

  teardown(() => {
    sinon.restore();
  });

  test('getName should return correct command name', () => {
    assert.strictEqual("kiota.workspace.deleteItem", command.getName());
  });

  test('execute should show success message and refresh workspace on success', async () => {
    const deleteItemStub = sinon.stub(command as any, 'deleteItem').resolves([{ message: 'removed successfully' }]);
    const showWarningMessageStub = sinon.stub(vscode.window, 'showWarningMessage').resolves({ title: "Yes" });
    const showInformationMessageStub = sinon.stub(vscode.window, 'showInformationMessage').resolves();
    const executeCommandStub = sinon.stub(vscode.commands, 'executeCommand').resolves();

    await command.execute(workspaceTreeItem);

    assert.strictEqual(deleteItemStub.calledOnce, true);
    assert.strictEqual(showWarningMessageStub.calledOnceWith("Do you want to delete this item?", sinon.match("Yes"), sinon.match("No")), true);
    assert.strictEqual(showInformationMessageStub.calledOnceWith('test-item removed successfully.'), true);
    assert.strictEqual(executeCommandStub.calledOnceWith('kiota.workspace.refresh'), true);
  });
});