import * as vscode from 'vscode';

import { treeViewId } from "../constants";
import { OpenApiTreeProvider } from "../openApiTreeProvider";
import { updateTreeViewIcons } from '../util';
import { Command } from "./Command";

export class CloseDescriptionCommand extends Command {

  public constructor(private _openApiTreeProvider: OpenApiTreeProvider) {
    super();
  }

  public getName(): string {
    return `${treeViewId}.closeDescription`;
  }

  public async execute(): Promise<void> {
    const yesAnswer = vscode.l10n.t("Yes");
    const response = await vscode.window.showInformationMessage(
      vscode.l10n.t("Do you want to remove this API description?"),
      yesAnswer,
      vscode.l10n.t("No")
    );
    if (response === yesAnswer) {
      this._openApiTreeProvider.closeDescription();
      await updateTreeViewIcons(treeViewId, false);
    }
  }

}