import { defineConfig } from '@vscode/test-cli';

export default defineConfig({
    tests:[
        {
            files: 'out/test/**/*.test.js'
        }
    ],
    coverage: {
        includeAll: true,
        exclude: ["**/src/test", "**/dist"],
        reporter: ["text", "html", "json-summary", "lcov"],
    },
});
