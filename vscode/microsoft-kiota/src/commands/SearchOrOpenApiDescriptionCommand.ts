import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId } from "../constants";
import { getExtensionSettings } from "../extensionSettings";
import { OpenApiTreeProvider } from "../openApiTreeProvider";
import { searchDescription } from "../searchDescription";
import { searchSteps } from "../steps";
import { openTreeViewWithProgress } from "../utilities/file";
import { Command } from "./Command";

export class SearchOrOpenApiDescriptionCommand extends Command {
  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;

  public constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  async execute(): Promise<void> {
    const yesAnswer = vscode.l10n.t("Yes, override it");
    if (!this._openApiTreeProvider.isEmpty() && this._openApiTreeProvider.hasChanges()) {
      const response = await vscode.window.showWarningMessage(
        vscode.l10n.t(
          "Before adding a new API description, consider that your changes and current selection will be lost."),
        yesAnswer,
        vscode.l10n.t("Cancel")
      );
      if (response !== yesAnswer) {
        return;
      }
    }
    const config = await searchSteps(x => vscode.window.withProgress({
      location: vscode.ProgressLocation.Notification,
      cancellable: false,
      title: vscode.l10n.t("Searching...")
    }, (progress, _) => {
      const settings = getExtensionSettings(extensionId);
      return searchDescription(this._context, x, settings.clearCache);
    }));
    if (config.descriptionPath) {
      await openTreeViewWithProgress(() => this._openApiTreeProvider.setDescriptionUrl(config.descriptionPath!));
    }
  }
}