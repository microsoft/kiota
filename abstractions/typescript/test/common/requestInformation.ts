/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { assert } from "chai";

import { RequestInformation } from "../../src/";

describe("RequestInformation", () => {
	it("Should set request information uri", () => {
        const requestInformation = new RequestInformation();
        const currentPath = "CURRENT_PATH";
        const pathSegment = "PATH_SEGMENT"
        requestInformation.setUri( currentPath, pathSegment, false);
        assert.isNotNull(URL);
        console.log(requestInformation.URI);
        assert.equal(requestInformation.URI, currentPath+ "" +pathSegment);
    });
});
