import { IntegrationParams } from "../utilities/deep-linking";

let deepLinkParams: Partial<IntegrationParams> = {};

export const getDeepLinkParams = () => deepLinkParams;
export const setDeepLinkParams = (params: Partial<IntegrationParams>) => {
  deepLinkParams = { ...deepLinkParams, ...params };
};