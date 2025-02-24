import stylisticTs from '@stylistic/eslint-plugin-ts';
import typescriptEslint from "@typescript-eslint/eslint-plugin";
import tsParser from "@typescript-eslint/parser";

export default [{
    ignores:
        [
            "**/.eslintrc.js",
            "**/*.mjs",
            "**/*.cjs",
            "**/*.js",
            "**/*.js.map",
            "**/*.d.ts",
            "**/node_modules/",
            "**/dist/",
            "**/tests/",
        ],
}, {
    files: ["**/*.ts"],
    plugins: {
        "@typescript-eslint": typescriptEslint,
        '@stylistic/ts': stylisticTs
    },

    languageOptions: {
        parser: tsParser,
        ecmaVersion: 6,
        sourceType: "module",

        parserOptions: {
            project: "./tsconfig.json",
        },
    },

    rules: {
        "@typescript-eslint/naming-convention": "warn",
        "@stylistic/ts/semi": "warn",
        "@typescript-eslint/no-floating-promises": "error",
        "@typescript-eslint/no-misused-promises": "error",
        curly: "warn",
        eqeqeq: "warn",
        "no-throw-literal": "warn",
        semi: "off",
    },
}];
