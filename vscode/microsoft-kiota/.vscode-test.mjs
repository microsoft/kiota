import { defineConfig } from '@vscode/test-cli';

export default defineConfig({
    tests: [
        {
            files: 'out/test/**/*.test.js',
            exclude: '**/*.spec.[tj]s'
        }
    ],
    coverage: {
        includeAll: true,
        exclude: ["**/src/test", "**/dist", "**/*.test.[tj]s", "**/*.ts", "**/*.spec.[tj]s", "**/src/kiotaInterop"],
        reporter: ["text-summary", "html", "json-summary", "lcov", "cobertura"],
    },
});
