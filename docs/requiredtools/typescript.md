# Required tools for TypeScript

- [NodeJS 14](https://nodejs.org/en/)
- [TypeScript](https://www.typescriptlang.org/) `npm i -g typescript`

## Initializing target projects

Before you can compile and run the target project, you will need to initialize it. After initializing the test project, you will need to add references to the [abstraction](../../abstractions/typescript) and the [authentication](../../authentication/typescript/azure), [http](../../http/typescript/fetch), [serialization](../../serialization/typescript/json) packages from the GitHub feed.

Clone a NodeJS/front end TypeScript starter like [this one](https://github.com/FreekMencke/node-typescript-starter).

```Shell
npm i @azure/identity node-fetch
```
