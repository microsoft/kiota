/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import resolve from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";

const config = [{
    input: ["dist/es/test/browser/index.js"],
    output: {
        file: "dist/es/test/index.js",
        format: "esm",
        name: "MicrosoftKiotaHttpFetchTest",
    },
    plugins: [
        commonjs({ include: ["node_modules/**"] }),
        resolve({
            browser: true,
            preferBuiltins: false,

        })
    ],
}];

export default config;