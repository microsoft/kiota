import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId, KIOTA_WORKSPACE_FILE } from "../../constants";
import { getExtensionSettings } from "../../extensionSettings";
import { ClientOrPluginProperties } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../openApiTreeProvider";
import { isClientType, isPluginType } from "../../util";
import { Command } from "../Command";
import { RegenerateService } from "./regenerate.service";

export class RegenerateCommand extends Command {
  
  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;
  private _clientKey: string;
  private _clientObject: ClientOrPluginProperties;
  private _workspaceGenerationType: string;

  public constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider,
    clientKey: string, clientObject: ClientOrPluginProperties, workspaceGenerationType: string) {
    super();
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
    this._clientKey = clientKey;
    this._clientObject = clientObject;
    this._workspaceGenerationType = workspaceGenerationType;
  }

  async execute(): Promise<void> {
    const settings = getExtensionSettings(extensionId);
    const workspaceJson = vscode.workspace.textDocuments.find(doc => doc.fileName.endsWith(KIOTA_WORKSPACE_FILE));
    if (workspaceJson && workspaceJson.isDirty) {
      await vscode.window.showInformationMessage(
        vscode.l10n.t("Please save the workspace.json file before re-generation."),
        vscode.l10n.t("OK")
      );
      return;
    }

    const regenerateService = new RegenerateService(this._context, this._openApiTreeProvider, this._clientKey, this._clientObject);
    if (isClientType(this._workspaceGenerationType)) {
      await regenerateService.regenerateClient(settings);
    }
    if (isPluginType(this._workspaceGenerationType)) {
      await regenerateService.regeneratePlugin(settings);
    }
  }
}