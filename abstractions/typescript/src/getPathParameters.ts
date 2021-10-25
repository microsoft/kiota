import { RequestInformation } from "./requestInformation";

export function getPathParameters(parameters: Map<string, unknown> | string | undefined) : Map<string, unknown> {
    const result = new Map<string, unknown>();
    if(typeof parameters === "string") {
        result.set(RequestInformation.raw_url_key, parameters);
    } else if(parameters instanceof Map) {
        parameters.forEach((v, k) => {
            result.set(k, v);
        });
    }
    return result;
}