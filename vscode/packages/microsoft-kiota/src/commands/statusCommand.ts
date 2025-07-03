import * as vscode from 'vscode';

import { statusBarCommandId } from "../constants";
import { Command } from "./Command";

export class StatusCommand extends Command {

  public getName(): string {
    return statusBarCommandId;
  }

  public async execute(): Promise<void> {
    const yesAnswer = vscode.l10n.t("Yes");
    const response = await vscode.window.showInformationMessage(
      vscode.l10n.t("Open installation instructions for kiota?"),
      yesAnswer,
      vscode.l10n.t("No")
    );
    if (response === yesAnswer) {
      await vscode.env.openExternal(vscode.Uri.parse("https://aka.ms/get/kiota"));
    }
  }

}