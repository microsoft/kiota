import { RequestInformation } from "./requestInformation";

export function getUrlTemplateParameters(parameters: Map<string, string> | string | undefined) : Map<string, string> {
    const result = new Map<string, string>();
    if(typeof parameters === "string") {
        result.set(RequestInformation.raw_url_key, parameters);
    } else if(parameters instanceof Map) {
        parameters.forEach((v, k) => {
            result.set(k, v);
        });
    }
    return result;
}