import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId, KIOTA_WORKSPACE_FILE } from "../../constants";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { getExtensionSettings } from "../../types/extensionSettings";
import { WorkspaceGenerationContext } from "../../types/WorkspaceGenerationContext";
import { isClientType, isPluginType } from "../../util";
import { confirmOverride } from "../../utilities/regeneration";
import { Command } from "../Command";
import { RegenerateService } from "./regenerate.service";

export class RegenerateCommand extends Command {

  constructor(private _context: ExtensionContext, private _openApiTreeProvider: OpenApiTreeProvider, private _kiotaOutputChannel: vscode.LogOutputChannel) {
    super();
  }

  public getName(): string {
    return `${extensionId}.regenerate`;
  }

  public async execute({ generationType, clientOrPluginKey, clientOrPluginObject }: WorkspaceGenerationContext): Promise<void> {
    const regenerate = await confirmOverride();
    if (!regenerate) {
      return;
    }

    const settings = getExtensionSettings(extensionId);
    const workspaceJson = vscode.workspace.textDocuments.find(doc => doc.fileName.endsWith(KIOTA_WORKSPACE_FILE));
    if (workspaceJson && workspaceJson.isDirty) {
      await vscode.window.showInformationMessage(
        vscode.l10n.t("Please save the workspace.json file before re-generation."),
        vscode.l10n.t("OK")
      );
      return;
    }

    const regenerateService = new RegenerateService(this._context, this._openApiTreeProvider, clientOrPluginKey, clientOrPluginObject, this._kiotaOutputChannel);
    if (isClientType(generationType)) {
      await regenerateService.regenerateClient(settings);
    }
    if (isPluginType(generationType)) {
      await regenerateService.regeneratePlugin(settings);
      if (workspaceJson) {
        await regenerateService.regenerateTeamsApp(workspaceJson, clientOrPluginKey);
      }
    }
    await vscode.commands.executeCommand('kiota.workspace.refresh');
  }
}