import stylisticTs from '@stylistic/eslint-plugin-ts'
import typescriptEslint from "@typescript-eslint/eslint-plugin";
import globals from "globals";
import tsParser from "@typescript-eslint/parser";
import path from "node:path";
import { fileURLToPath } from "node:url";
import js from "@eslint/js";
import { FlatCompat } from "@eslint/eslintrc";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const compat = new FlatCompat({
    baseDirectory: __dirname,
    recommendedConfig: js.configs.recommended,
    allConfig: js.configs.all
});

export default [{
    ignores: ["**/dist", "**/node_modules", "**/.vscode"],
}, ...compat.extends("prettier"), {
    files: ["**/*.ts"],
    plugins: {
        "@typescript-eslint": typescriptEslint,
        '@stylistic/ts': stylisticTs
    },

    languageOptions: {
        globals: {
            ...globals.node,
        },

        parser: tsParser,
        ecmaVersion: 5,
        sourceType: "module",

        parserOptions: {
            project: ["tsconfig.json", "tsconfig.eslint.json"],
        },
    },

    rules: {
        "@stylistic/ts/indent": ["error", 2],

        "@stylistic/ts/member-delimiter-style": ["error", {
            multiline: {
                delimiter: "semi",
                requireLast: true,
            },

            singleline: {
                delimiter: "semi",
                requireLast: false,
            },
        }],

        "@stylistic/ts/quotes": ["error", "single", {
            avoidEscape: true,
        }],

        "@stylistic/ts/semi": ["error", "always"],
        "comma-dangle": ["error", "always-multiline"],
        "max-classes-per-file": "off",
        "no-console": "error",

        "no-multiple-empty-lines": ["error", {
            max: 1,
        }],

        "no-redeclare": "error",
        "no-return-await": "error",
        "prefer-const": "error",
    },
}];
