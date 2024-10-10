import { ClientOrPluginProperties } from "../kiotaInterop";

export interface WorkspaceGenerationContext {
  clientOrPluginKey: string;
  clientOrPluginObject: ClientOrPluginProperties;
  generationType: string;
}
