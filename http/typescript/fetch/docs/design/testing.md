## Testing for node and browser

### Testing for node

- Tests targeting the node environment are in `/test/node` and `/test/common` folder and use `mocha` and `chai` JS libraries.
- Test formats:
    - script to test `CommonJS` modules: `npm run test:cjs`
    - script to test `ES` modules:  `npm run test:es`
- Examples of node environment specific tests: Test `DefaultFetchHandler` using `node-fetch` library.
 

### Testing for browser


- Tests targeting the node environment are in `/test/browser` and `/test/common` folder and use `mocha` and `chai` JS libraries.
- To test for browsers, the tests and the source code are bundled using `rollup` and the bundled file is tested using `karma`.
- Test formats:
    - script to test: `npm run karma`.
- Examples of node environment specific tests: Test `DefaultFetchHandler` using dom - `fetch`.

---
**NOTE**

The bundled file considers the `package.json browser spec` during the rollup process. The entry point of the source code for the tests will be `src/browser/index.js` and the `package.json browser spec` file mapping should work.   
---
