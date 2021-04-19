import { AuthenticationProvider, HttpCore as IHttpCore, Parsable, RequestInfo, ResponseHandler } from '@microsoft/kiota-abstractions';
import { fetch, Headers as FetchHeadersCtor } from 'cross-fetch';
import { RequestInit as FetchRequestInit, Headers as FetchHeaders } from 'cross-fetch/lib.fetch';
import { JsonParseNode } from './serialization';
import { URLSearchParams } from 'url';
export class HttpCore implements IHttpCore {
    private static readonly authorizationHeaderKey = "Authorization";
    /**
     *
     */
    public constructor(public readonly authenticationProvider: AuthenticationProvider) {
        if(!authenticationProvider) {
            throw new Error('authentication provider cannot be null');
        }
    }
    public sendAsync = async <ModelType extends Parsable<ModelType>>(requestInfo: RequestInfo, type: new() => ModelType, responseHandler: ResponseHandler | undefined): Promise<ModelType> => {
        if(!requestInfo) {
            throw new Error('requestInfo cannot be null');
        }
        if(!requestInfo.URI) {
            throw new Error('uri cannot be null');
        }
        if(!requestInfo.headers?.has(HttpCore.authorizationHeaderKey)) {
            const token = await this.authenticationProvider.getAuthorizationToken(requestInfo.URI);
            if(!token) {
                throw new Error('Could not get an authorization token');
            }
            if(!requestInfo.headers) {
                requestInfo.headers = new Map<string, string>();
            }
            requestInfo.headers?.set(HttpCore.authorizationHeaderKey, `Bearer ${token}`);
        }
        const request = this.getRequestFromRequestInfo(requestInfo);
        const response = await fetch(this.getRequestUrl(requestInfo), request);
        if(responseHandler) {
            return await responseHandler.handleResponseAsync(response);
        } else {
            const payload = await response.json();
            const rootNode = new JsonParseNode(payload);
            const result = rootNode.getObjectValue(type);
            return result as unknown as ModelType;
        }
    }
    private getRequestFromRequestInfo = (requestInfo: RequestInfo): FetchRequestInit => {
        const request = {
            method: requestInfo.httpMethod?.toString(),
            headers: new FetchHeadersCtor(),
            body: requestInfo.content,
        } as FetchRequestInit;
        requestInfo.headers?.forEach((v, k) => (request.headers as FetchHeaders).set(k, v));
        return request;
    }
    private getRequestUrl = (requestInfo: RequestInfo) : string => {
        let url = requestInfo.URI ?? '';
        if(requestInfo.queryParameters?.size ?? -1 > 0) {
            const queryParametersBuilder = new URLSearchParams();
            requestInfo.queryParameters?.forEach((v, k) => {
                queryParametersBuilder.append(k, `${v}`);
            });
            url = url + '?' + queryParametersBuilder.toString();
        }
        return url;
    }

}