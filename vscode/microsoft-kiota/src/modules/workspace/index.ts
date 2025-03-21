import { ClientOrPluginProperties } from "@microsoft/kiota"
  ;
import WorkspaceContentService from "./workspaceContentService";

export interface WorkspaceContent {
  version: string;
  clients: Record<string, ClientOrPluginProperties>;
  plugins: Record<string, ClientOrPluginProperties>;
}
export { WorkspaceContentService };

