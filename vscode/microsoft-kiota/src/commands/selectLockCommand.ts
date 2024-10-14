import { extensionId } from "../constants";
import { OpenApiTreeProvider } from "../openApiTreeProvider";
import { loadWorkspaceFile } from "../utilities/file";
import { Command } from "./Command";

export class SelectLockCommand extends Command {
  constructor(private _openApiTreeProvider: OpenApiTreeProvider) {
    super();
  }

  public getName(): string {
    return `${extensionId}.selectLock`;
  }

  public async execute(node: any): Promise<void> {
    await loadWorkspaceFile(node, this._openApiTreeProvider);
  }

}