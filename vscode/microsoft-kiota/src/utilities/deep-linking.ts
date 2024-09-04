import { IntegrationParams } from "../util";

export function isDeeplinkEnabled(deepLinkParams: Partial<IntegrationParams>): boolean {
  return Object.keys(deepLinkParams).length > 0;
}
