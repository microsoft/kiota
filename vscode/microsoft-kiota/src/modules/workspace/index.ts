import { ClientOrPluginProperties } from "../../kiotaInterop";
import WorkspaceContentService from "./workspaceContentService";

export interface WorkspaceContent {
  version: string;
  clients: Record<string, ClientOrPluginProperties>;
  plugins: Record<string, ClientOrPluginProperties>;
}
export { WorkspaceContentService };
