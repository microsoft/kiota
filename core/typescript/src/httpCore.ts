import { AuthenticationProvider, HttpCore as IHttpCore, RequestInfo, ResponseHandler } from '@microsoft/kiota-abstractions';
import fetch from 'isomorphic-fetch';
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
    public sendAsync = async <ModelType>(requestInfo: RequestInfo, responseHandler: ResponseHandler | undefined): Promise<ModelType> => {
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
            return {} as ModelType; //TODO call default respone handler which will handle deserialization
        }
    }
    private getRequestFromRequestInfo = (requestInfo: RequestInfo): object => {
        const request = {
            method: requestInfo.httpMethod?.toString(),
            headers: new Map<string, string>(),
            body: requestInfo.content,
        };
        requestInfo.headers?.forEach((v, k) => request.headers.set(k, v));
        return request;
    }
    private getRequestUrl = (requestInfo: RequestInfo) : string => {
        let url = requestInfo.URI ?? '';
        if(requestInfo.queryParameters?.size ?? -1 > 0) {
            url+= '?';
            requestInfo.queryParameters?.forEach((v, k) => {
                url += k;
                if(v) {
                    url += `=${v}`;
                }
                url+='&';
            });
            url = url.substring(0, url.length -1); // removing the last &
        }
        return url;
    }

}