import { ExtensionContext } from "vscode";

import { OpenApiTreeProvider } from "../../openApiTreeProvider";
import { GenerateState } from "../../steps";
import { Command } from "../Command";
import { GeneratedOutputState } from "../GeneratedOutputState";
import { displayGenerationResults } from "./generation-results";

export class DisplayGenerationResultsCommand extends Command {
  private _context: ExtensionContext;
  private _openApiTreeProvider: OpenApiTreeProvider;

  public constructor(context: ExtensionContext, openApiTreeProvider: OpenApiTreeProvider) {
    super();
    this._context = context;
    this._openApiTreeProvider = openApiTreeProvider;
  }

  async execute(config: Partial<GenerateState>): Promise<void> {
    const generatedOutput = this._context.workspaceState.get<GeneratedOutputState>('generatedOutput');
    if (generatedOutput) {
      const { outputPath } = generatedOutput;
      await displayGenerationResults(config, outputPath, this._openApiTreeProvider);
      // Clear the state 
      void this._context.workspaceState.update('generatedOutput', undefined);
    }
  }

  
}