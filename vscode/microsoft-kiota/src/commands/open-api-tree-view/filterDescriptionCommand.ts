import { OpenApiTreeProvider } from "../../openApiTreeProvider";
import { filterSteps } from "../../steps";
import { Command } from "../Command";

export class FilterDescriptionCommand extends Command {

  private _openApiTreeProvider: OpenApiTreeProvider;

  constructor(openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._openApiTreeProvider = openApiTreeProvider;
  }

  public getName(): string {
    throw new Error("Method not implemented.");
  }

  public async execute(): Promise<void> {
    await filterSteps(this._openApiTreeProvider.filter,
      x => this._openApiTreeProvider.filter = x);
  }

}
