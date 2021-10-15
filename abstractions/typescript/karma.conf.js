module.exports = function(config) {
    config.set({
        frameworks: ["mocha", "chai", "karma-typescript"],
        files: [{ pattern: "dist/es/test/index.js", type: "module" }],
        preprocessors: {
            "**/*.ts": ["karma-typescript"],
        },
        karmaTypescriptConfig: {
            tsconfig: "./tsconfig.cjs.json",
        },
        browsers: ["ChromeHeadless"],
    });
};