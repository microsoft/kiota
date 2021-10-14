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
    input: ["dist/es/test/browser/index.js"],
    output: {
        file: "dist/es/test/rolledup.js",
        format: "esm",
        name: "MicrosoftGraph",
    },
    plugins: [
        commonjs({ include: ["node_modules/**"] }),
        resolve({
            browser: true,
            preferBuiltins: false,

        }),
        // terser({
        //     format: {
        //         comments: false,
        //         preamble: copyRight,
        //     },
        // }),
    ],
}];

export default config;