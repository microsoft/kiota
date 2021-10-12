module.exports = function(config) {
    config.set({
        frameworks: ["mocha", "chai", "karma-typescript"],
        //files: ["src/browser/index.ts", "test/common/**/*.ts", "test/browser/**/*.ts", "test/testUtils.ts"],
        files: [{ pattern: "dist/es/test/rolledup.js", type: "module" }],
        preprocessors: {
            "**/*.ts": ["karma-typescript"],
        },
        karmaTypescriptConfig: {
            tsconfig: "./tsconfig.cjs.json",
        },
        browsers: ["ChromeHeadless"],
    });
};