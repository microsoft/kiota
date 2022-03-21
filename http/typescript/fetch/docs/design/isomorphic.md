## Terms -

-   bundler - Module bundlers are tools frontend developers used to bundle JavaScript modules into a single JavaScript files that can be executed in the browser.

-   rollup - rollup.js is the module bundler that the JS SDK uses.

-   package.json fields -

    -   main - The main field is a module ID that is the primary entry point to your program. Points to the CJS modules.

    -   module - The module field is not an official npm feature but a common convention among bundlers to designate how to import an ESM version of a library. Points to the ES modules.

    -   browser - If the module is meant to be used client-side, the browser field should be used instead of the main field.

## Isomorphic library
The Kiota libraries are isomorphic, that is, this library runs in node and browser. 

To achieve the isomorphism, the library is set up in the following ways:

                      TypeScript Source Code
                                / \
                      Transpiles into JavaScript
                             'dist' folder
                               /   \
                     CJS module     ES modules
1.  The library supports `CommonJS` modules and `ES` modules.`CommonJS` formats are popular in the node enviroment. 
    `main` - `dist/es/src/index.js` 
    `module` - `dist/es/src/index.js`

2. Entry point for the browser - `dist/es/src/browser/index.js`.

Often times the library will have code which is different for the browser and node environments. 
Examples:
- Browsers use a global `fetch` defined in the `DOM` while node relies on an external library such as `node-fetch` or `undici`.
- `ReadableStream` are different interfaces in node and browser.

To manage such differences, separate files are maintained when the code can be different for node or browser. 

For example: 
- The  `DefaultFetchHandler` uses `node-fetch` for node and the `dom` fetch for browser. 
- Map this difference as follows: 

```json
"browser": {
        "./dist/es/src/index.js": "./dist/es/src/browser/index.js",
        "./dist/es/src/utils/utils.js": "./dist/es/src/utils/browser/utils.js",
        "./dist/es/src/middlewares/defaultFetchHandler.js": "./dist/es/src/middlewares/browser/defaultFetchHandler.js",
        "./dist/es/src/middlewares/middlewareFactory.js": "./dist/es/src/middlewares/browser/middlewareFactory.js",
        "./dist/cjs/src/middlewares/middlewareFactory.js": "./dist/es/src/middlewares/browser/middlewareFactory.js"
}
```

The applications or library using `kiota-http-fetch library` will have bundlers that use this `package.json browser` mapping if the target environment is set to browser.


## Use of `lib:dom` and `@types/node`

Typescript isomorphic libraries often rely on the DOM definitions and `@types/node` as compile-time dependencies. 

Example 1:
```json
// tsconfig.json in the isomorphic library

{
    "compileroptions": {
        "lib":"dom"
    }
}
```
Issues with example 1: 
    - This configuration makes DOM definitions globally available as a compile time dependency during development and it is not shipped with the library. However, when a node only application uses the isomorphic library, the DOM definitions will not be available and this causes Typescript  compile time errors.
    - [Issue example: Removed dom lib dependency](https://github.com/prisma-labs/graphql-request/issues/26)
    - [PR example: azure core-http](https://github.com/Azure/azure-sdk-for-js/pull/7500)
    

Example 2:
```
// example of use of @type/nodes
const readableStream : NodeJS.ReadableStream

or

global ambient declarations at the top of the file
/// <reference lib="dom" /> 
/// <reference lib="node" /> 
```

Issues with example 2:
 - When transpiling a direct reference to `@types/node`, such as  `NodeJS.ReadableStream`, the transpiled code contains a global ambient declaration `/// <reference lib="node" />`. The global ambient declarations leak the typings to the user code. 
 - Example of issues:
    - [Removes reference to node typings and stubs dom interfaces](https://github.com/aws/aws-sdk-js/pull/1228) 
    - [Typings polutes global space with DOM types ](https://github.com/node-fetch/node-fetch/issues/1285)


Other issues:
Typescript is yet capture some of complete dynamic support that JavaScript provides. An example of this is [Node browser resolution strategy](https://github.com/microsoft/TypeScript/issues/7753) since today the compiler cannot type check based on the environment.

### Solution to this:

Approaches for this problem as observed in other isomorphic libraries:
- A good reference to a very optimal solution is here: [PR example: azure core-http](https://github.com/Azure/azure-sdk-for-js/pull/7500)
- A common approch that is taken is to create empty interfaces or shims. Then, if the user includes dom or the required definitions, the interfaces are merged. Reference [Removes reference to node typings and stubs dom interfaces](https://github.com/aws/aws-sdk-js/pull/1228) 

##### DOM shims and Fetch definitions
The `kiota-http-fetch library` relies on the DOM definitions such as `RequestInit`, `RequestInfo`. 

1. Maintain and export `dom.shim.d.ts` containing empty interfaces. This way the user can compile their code with the library without having a DOM library.

```typescript
interface Request { }
interface RequestInit { }
interface Response { }
interface Headers { }
interface ReadableStream { }
interface fetch { }
```

2. The library code relies on interface definitions and interface properties such as `Request.body`, `Request.headers`. Introduced the  `FetchDefinitions.ts` to achieve this. `FetchDefinitions.ts` contains the following intersection types: 
- `FetchRequestInit`
- `FetchHeadersInit`
- `FetchResponse`

These types are redefined so that definitions are available and can be set isomorphically.

For example: 

``` typescript
export type FetchRequestInit = Omit<RequestInit, "body" | "headers" | "redirect" | "signal"> & {
	/**
	 * Request's body
	 * Expected type in case of dom - ReadableStream | XMLHttpRequestBodyInit|null
	 * Expected type in case of node-fetch - | Blob | Buffer | URLSearchParams | NodeJS.ReadableStream | string|null
	 */
	body?: unknown;
}
````

##### ReadableStreamContent

- `ReadableStream` are different interfaces in node and browser.
-  Introducing `ReadableStreamContent`, empty interface expecting the type of NodeJS.ReadableStream or dom ReadableStream
 * Node example: import {Readable} from "stream"; const readableStream = new Readable();
 * Browser example: const readableStream = new ReadableStream();

Alternatives considered to manage this difference:
- `content - NodeJS.ReadableStream | ReadableStream`. This transpiles to use the global ambient declaration of `type = nodes` which causes the global leaking issues discussed above.
- `content - ReadableStream` and maintaining an empty interface with the same name, that is, ReadableStream causes a mismatch error if content is set to NodeJS.Readable and DOM definitions are present at the same time. 
- Due to the lack of `node browser resolution strategy` in the typings, we cannot maintain separate node and browser typings files for `ReadableStream` interface.
