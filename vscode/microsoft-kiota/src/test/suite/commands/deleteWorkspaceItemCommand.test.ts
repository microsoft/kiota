import * as vscode from 'vscode';

import { DeleteWorkspaceItemCommand } from '../../../commands/deleteWorkspaceItem/deleteWorkspaceItemCommand';
import * as treeModule from "../../../providers/openApiTreeProvider";
import * as sharedServiceModule from '../../../providers/sharedService';
import { WorkspaceTreeItem } from '../../../providers/workspaceTreeProvider';

describe('DeleteWorkspaceItemCommand Tests', () => {
  let context: vscode.ExtensionContext;
  let outputChannel: vscode.LogOutputChannel;
  let command: DeleteWorkspaceItemCommand;
  let workspaceTreeItem: WorkspaceTreeItem;

  beforeAll(() => {
    context = { extension: { packageJSON: { telemetryInstrumentationKey: 'test-key' } } } as any;
    outputChannel = { appendLine: jest.fn() } as any;
    const treeProvider = jest.fn(() => ({} as treeModule.OpenApiTreeProvider));
    const stubbedSharedService = jest.fn(() => ({} as sharedServiceModule.SharedService));
    command = new DeleteWorkspaceItemCommand(context, treeProvider(), outputChannel, stubbedSharedService());
    workspaceTreeItem = { label: 'test-item', category: 'plugin' } as any;
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  test('getName should return correct command name', () => {
    expect("kiota.workspace.deleteItem").toEqual(command.getName());
  });

  test('execute should show success message and refresh workspace on success', async () => {
    const yesAnswer: vscode.MessageItem = { title: vscode.l10n.t("Yes") };

    const showWarningMessageStub = jest.spyOn(vscode.window, 'showWarningMessage').mockResolvedValue(yesAnswer);
    const showInformationMessageStub = jest.spyOn(vscode.window, 'showInformationMessage').mockResolvedValue(undefined);
    const deleteItemStub = jest.spyOn(command as any, 'deleteItem').mockResolvedValue({ isSuccess: true, logs: [{ message: 'removed successfully' }] });

    await command.execute(workspaceTreeItem);

    expect(showWarningMessageStub).toHaveBeenCalledTimes(1);
    expect(showInformationMessageStub).toHaveBeenCalledTimes(1);
    expect(deleteItemStub).toHaveBeenCalledTimes(1);
  });

});