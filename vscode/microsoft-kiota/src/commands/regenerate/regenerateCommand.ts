import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId, KIOTA_WORKSPACE_FILE } from "../../constants";
import { getExtensionSettings } from "../../extensionSettings";
import { getWorkspaceGenerationType } from "../../handlers/workspaceGenerationTypeHandler";
import { ClientOrPluginProperties } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../openApiTreeProvider";
import { isClientType, isPluginType } from "../../util";
import { confirmOverride } from "../../utilities/regeneration";
import { Command } from "../Command";
import { RegenerateService } from "./regenerate.service";

export class RegenerateCommand extends Command {
  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    return `${extensionId}.regenerate`;
  }

  public async execute({ clientKey, clientObject }: { clientKey: string, clientObject: ClientOrPluginProperties }): Promise<void> {
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

    const regenerateService = new RegenerateService(this._context, this._openApiTreeProvider, clientKey, clientObject);
    const generationType = getWorkspaceGenerationType();
    if (isClientType(generationType)) {
      await regenerateService.regenerateClient(settings);
    }
    if (isPluginType(generationType)) {
      await regenerateService.regeneratePlugin(settings);
    }
  }

}