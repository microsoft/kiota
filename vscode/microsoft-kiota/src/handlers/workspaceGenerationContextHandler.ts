import { ClientOrPluginProperties } from "../kiotaInterop";

export interface WorkspaceGenerationContext {
  clientOrPluginKey: string;
  clientOrPluginObject: ClientOrPluginProperties;
  generationType: string;
}

let workspaceGenerationContext: WorkspaceGenerationContext;
export const getWorkspaceGenerationContext = () => workspaceGenerationContext;
export const setWorkspaceGenerationContext = (params: Partial<WorkspaceGenerationContext>) => {
  workspaceGenerationContext = { ...workspaceGenerationContext, ...params };
};