import { defineConfig } from '@vscode/test-cli';

export default defineConfig({
    files: 'out/test/**/*.test.js',
    includeAll: true,
    exclude: ["out/src/test/**/*.test.*", "**/dist"],
    reporter: ["text", "html", "json-summary", "lcov"],
});
