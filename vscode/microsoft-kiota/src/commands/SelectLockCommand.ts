import { extensionId } from "../constants";
import { OpenApiTreeProvider } from "../providers/openApiTreeProvider";
import { loadLockFile } from "../utilities/file";
import { Command } from "./Command";

export class SelectLockCommand extends Command {

  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public toString(): string {
    return `${extensionId}.selectLock`;
  }

  execute(x: any): void {
    void loadLockFile(x, this._openApiTreeProvider);
  }
};