/**
 * -------------------------------------------------------------------------------------------
 * Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
 * See License in the project root for license information.
 * -------------------------------------------------------------------------------------------
 */

import { terser } from "rollup-plugin-terser";
import resolve from "@rollup/plugin-node-resolve";
import commonjs from "@rollup/plugin-commonjs";

const copyRight = `/**
* -------------------------------------------------------------------------------------------
* Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.
* See License in the project root for license information.
* -------------------------------------------------------------------------------------------
*/`;

const config = [{
    input: ["dist/es/test/commmon/**/*.js,dist/es/test/browser/**/*.js"],
    output: {
        file: "dist/es/test/rolledup.js",
        format: "esm",
        name: "MicrosoftGraph",
    },
    external: ['@microsoft/kiota-abstractions'],
    plugins: [
        // resolve({
        //     browser: true,
        //     preferBuiltins: false,

        // }),
        commonjs({ include: "node_modules/**" }),
        terser({
            format: {
                comments: false,
                preamble: copyRight,
            },
        }),
    ],
}];

export default config;