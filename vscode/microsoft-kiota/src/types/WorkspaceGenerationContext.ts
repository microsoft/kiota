import { ClientOrPluginProperties } from "@microsoft/kiota";

export interface WorkspaceGenerationContext {
  clientOrPluginKey: string;
  clientOrPluginObject: ClientOrPluginProperties;
  generationType: string;
}
