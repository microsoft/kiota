const Module = require('module');

const originalResolveFilename = Module._resolveFilename;
Module._resolveFilename = function (request, parent, isMain, options) {
    if (request.startsWith('.') && request.endsWith('.js') && parent?.filename?.endsWith('.ts')) {
        try {
            return originalResolveFilename.call(this, request.slice(0, -3), parent, isMain, options);
        } catch {
            // Keep the original request if there is no matching TypeScript source.
        }
    }
    return originalResolveFilename.call(this, request, parent, isMain, options);
};

module.exports = async (...args) => require('./setup.ts').default(...args);
