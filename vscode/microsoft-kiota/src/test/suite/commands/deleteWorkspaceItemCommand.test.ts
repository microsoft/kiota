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
    // Create MessageItem objects
    const yesAnswer: vscode.MessageItem = { title: vscode.l10n.t("Yes") };

    // Mock the showWarningMessage method
    const showWarningMessageStub = sinon.stub(vscode.window, 'showWarningMessage').resolves(yesAnswer);
    const showInformationMessageStub = sinon.stub(vscode.window, 'showInformationMessage').resolves();
    const executeCommandStub = sinon.stub(vscode.commands, 'executeCommand').resolves();

    await command.execute(workspaceTreeItem);

    console.log('showWarningMessageStub.calledOnce:', showWarningMessageStub.calledOnce);
    console.log('showInformationMessageStub.calledOnce:', showInformationMessageStub.calledOnce);
    console.log('executeCommandStub.calledWith("kiota.workspace.refresh"):', executeCommandStub.calledWith('kiota.workspace.refresh'));

    assert.strictEqual(showWarningMessageStub.calledOnce, true);
    assert.strictEqual(showInformationMessageStub.calledOnce, true);
    assert.strictEqual(executeCommandStub.calledWith('kiota.workspace.refresh'), true);
  });
});