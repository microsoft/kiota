import * as vscode from "vscode";
import { ExtensionContext } from "vscode";

import { extensionId, treeViewId } from "../../constants";
import { getExtensionSettings } from "../../extensionSettings";
import { ClientOrPluginProperties } from "../../kiotaInterop";
import { OpenApiTreeProvider } from "../../providers/openApiTreeProvider";
import { GenerateState } from "../../steps";
import { isClientType, isPluginType } from "../../util";
import { Command } from "../Command";
import { RegenerateService } from "./regenerate.service";

export class RegenerateButtonCommand extends Command {
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

  public toString(): string {
    return `${treeViewId}.regenerateButton`;
  }

  async execute(config: Partial<GenerateState>): Promise<void> {
    if (!this._clientKey || this._clientKey === '') {
      this._clientKey = config.clientClassName || config.pluginName || '';
    }
    if (!config) {
      config = {
        outputPath: this._clientObject.outputPath,
        clientClassName: this._clientKey,
      };
    }
    const settings = getExtensionSettings(extensionId);
    const selectedPaths = this._openApiTreeProvider.getSelectedPaths();
    if (selectedPaths.length === 0) {
      await vscode.window.showErrorMessage(
        vscode.l10n.t("No endpoints selected, select endpoints first")
      );
      return;
    }

    const regenerateService = new RegenerateService(this._context, this._openApiTreeProvider, this._clientKey, this._clientObject);
    if (isClientType(this._workspaceGenerationType)) {
      await regenerateService.regenerateClient(settings, selectedPaths);
    }
    if (isPluginType(this._workspaceGenerationType)) {
      await regenerateService.regeneratePlugin(settings, selectedPaths);
    }
  }
}
