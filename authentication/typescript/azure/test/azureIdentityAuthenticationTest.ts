/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { AccessToken, ClientSecretCredential } from "@azure/identity";
import { BaseBearerTokenAuthenticationProvider, RequestInformation } from "@microsoft/kiota-abstractions";
import { assert } from "chai";
import * as sinon from "sinon";
import { AzureIdentityAuthenticationProvider } from "../src";

import { AzureIdentityAccessTokenProvider } from "../src/azureIdentityAccessTokenProvider";

describe("Test authentication using @azure/identity", () => {
    const tenantId = "0000-1111-2222-3333";
    const clientId = "CLIENT_ID";
    const clientSecret = "CLIENT_SECRET";
    const scopes = ["test_scopes"];
    it("AccessToken is returned correctly from getAuthorizationToken function", async () => {
        const clientCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        if (typeof clientCredential.getToken !== "function") {
            throw new Error("Method definition for getToken is not found");
        }

        const accessToken: AccessToken = { token: "dummy_valid_token", expiresOnTimestamp: 1 };

        const moq = sinon.mock(clientCredential);
        moq.expects("getToken").resolves(accessToken);
        const accessTokenProvider = new AzureIdentityAccessTokenProvider(clientCredential, scopes);
        const access = await accessTokenProvider.getAuthorizationToken();
        assert.equal(access, accessToken.token);
    });

    it("AccessToken is appended correctly in header by AzureIdentityAuthenticationProvider", async () => {
        const clientCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        if (typeof clientCredential.getToken !== "function") {
            throw new Error("Method definition for getToken is not found");
        }

        const accessToken: AccessToken = { token: "dummy_valid_token", expiresOnTimestamp: 1 };

        const moq = sinon.mock(clientCredential);
        moq.expects("getToken").resolves(accessToken);
        const request: RequestInformation = new RequestInformation();
        const tokenCredentialAuthenticationProvider = new AzureIdentityAuthenticationProvider(clientCredential, scopes);
        await tokenCredentialAuthenticationProvider.authenticateRequest(request);
        assert.equal(request.headers.get("Authorization"), "Bearer " + accessToken.token);
    });

    it("AccessToken is appended correctly in header by BaseBearerTokenAuthenticationProvider", async () => {
        const clientCredential = new ClientSecretCredential(tenantId, clientId, clientSecret);

        if (typeof clientCredential.getToken !== "function") {
            throw new Error("Method definition for getToken is not found");
        }

        const accessToken: AccessToken = { token: "dummy_valid_token", expiresOnTimestamp: 1 };

        const moq = sinon.mock(clientCredential);
        moq.expects("getToken").resolves(accessToken);
        const request: RequestInformation = new RequestInformation();
        const accessTokenProvider = new AzureIdentityAccessTokenProvider(clientCredential, scopes);
        const tokenCredentialAuthenticationProvider = new BaseBearerTokenAuthenticationProvider(accessTokenProvider);
        await tokenCredentialAuthenticationProvider.authenticateRequest(request);
        assert.equal(request.headers.get("Authorization"), "Bearer " + accessToken.token);
    });
});
